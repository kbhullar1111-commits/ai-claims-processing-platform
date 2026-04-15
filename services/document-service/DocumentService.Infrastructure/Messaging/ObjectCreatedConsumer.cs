using BuildingBlocks.Contracts.Documents;
using DocumentService.Domain.Entities;
using DocumentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net;
using System.Text;
using System.Text.Json;

namespace DocumentService.Infrastructure.Messaging;

public class ObjectCreatedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<ObjectCreatedConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public ObjectCreatedConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<ObjectCreatedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory()
        {
            HostName = _options.Host,
            UserName = _options.Username,
            Password = _options.Password,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Set up a dead-letter exchange and queue to handle messages that fail processing repeatedly.
        _channel.ExchangeDeclare(_options.MinioDeadLetterExchangeName, ExchangeType.Direct, durable: true);

        _channel.QueueDeclare(_options.MinioObjectCreatedDeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        _channel.QueueBind(
            _options.MinioObjectCreatedDeadLetterQueueName,
            _options.MinioDeadLetterExchangeName,
            routingKey: _options.MinioDeadLetterRoutingKey);

        // Retry topology: failed messages are published to the retry exchange with
        // a per-message TTL. After TTL expires, RabbitMQ dead-letters them back to
        // the main queue for another processing attempt.
        _channel.ExchangeDeclare(_options.MinioRetryExchangeName, ExchangeType.Direct, durable: true);

        var retryQueueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", string.Empty },
            { "x-dead-letter-routing-key", _options.MinioObjectCreatedQueueName }
        };

        _channel.QueueDeclare(
            _options.MinioObjectCreatedRetryQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: retryQueueArgs);

        _channel.QueueBind(
            _options.MinioObjectCreatedRetryQueueName,
            _options.MinioRetryExchangeName,
            routingKey: _options.MinioRetryRoutingKey);

        var args = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", _options.MinioDeadLetterExchangeName },
            { "x-dead-letter-routing-key", _options.MinioDeadLetterRoutingKey }
        };

        // MinIO emits object-created notifications to RabbitMQ. This service owns
        // a dedicated queue that receives those raw notifications.
        _channel.ExchangeDeclare(_options.MinioExchangeName, ExchangeType.Fanout, durable: true);
        _channel.QueueDeclare(_options.MinioObjectCreatedQueueName, durable: true, exclusive: false, autoDelete: false, arguments: args);
        _channel.QueueBind(_options.MinioObjectCreatedQueueName, _options.MinioExchangeName, routingKey: string.Empty);
        
        // Process one message at a time so document creation and outbox persistence
        // stay simple and easier to reason about.
        _channel.BasicQos(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += HandleMessageAsync;
        _channel.BasicConsume(_options.MinioObjectCreatedQueueName, autoAck: false, consumer);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _channel?.Close();
            _connection?.Close();
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }

    private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        if (_channel is null)
            return;

        try
        {
            //if (_options.ThrowTestException)
                //throw new InvalidOperationException("Forced test exception from ObjectCreatedConsumer to validate retry and DLQ behavior.");

            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
            var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            var message = JsonSerializer.Deserialize<MinioObjectCreated>(json);
            var document = CreateDocument(message);

            if (document is null)
            {
                _logger.LogWarning("Skipping MinIO notification with unsupported payload: {Payload}", json);
                _channel.BasicAck(eventArgs.DeliveryTag, false);
                return;
            }

            // MinIO can redeliver notifications, so ObjectKey is the idempotency boundary.
            // If we already stored a document for that object, we acknowledge and stop.
            var exists = await db.Documents.AnyAsync(
                x => x.ObjectKey == document.ObjectKey,
                CancellationToken.None);

            if (exists)
            {
                _logger.LogInformation("Duplicate object key received: {Key}", document.ObjectKey);

                // OPTIONAL: still publish event if you want saga to re-evaluate
                // (depends on your design choice)
                _channel.BasicAck(eventArgs.DeliveryTag, false);
                return;
            }

            db.Documents.Add(document);

            // The outbox row is stored in the same database transaction as the document.
            // That guarantees we never publish DocumentUploaded unless the document row exists.
            db.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = "DocumentUploaded",
                Payload = JsonSerializer.Serialize(new DocumentUploaded(
                    document.Id,
                    document.ClaimId,
                    document.DocumentType,
                    document.UploadedAt
                )),
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();

            // Ack only after the document row and outbox row are committed.
            _channel.BasicAck(eventArgs.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed processing message");

            try
            {
                var retryCount = GetRetryCount(eventArgs.BasicProperties?.Headers);

                if (retryCount >= _options.MaxRetryAttempts)
                {
                    _logger.LogWarning(
                        "Moving message to DLQ after max retries. DeliveryTag={DeliveryTag}, RetryCount={RetryCount}",
                        eventArgs.DeliveryTag,
                        retryCount);

                    _channel.BasicReject(eventArgs.DeliveryTag, requeue: false);
                    return;
                }

                var nextRetryCount = retryCount + 1;
                var delayMs = GetRetryDelayMilliseconds(retryCount);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.Expiration = delayMs.ToString();
                properties.Headers = new Dictionary<string, object>
                {
                    { "x-retry-count", nextRetryCount }
                };

                _channel.BasicPublish(
                    exchange: _options.MinioRetryExchangeName,
                    routingKey: _options.MinioRetryRoutingKey,
                    basicProperties: properties,
                    body: eventArgs.Body);

                _logger.LogWarning(
                    "Scheduled retry for message. DeliveryTag={DeliveryTag}, RetryCount={RetryCount}, DelayMs={DelayMs}",
                    eventArgs.DeliveryTag,
                    nextRetryCount,
                    delayMs);

                _channel.BasicAck(eventArgs.DeliveryTag, false);
            }
            catch (Exception retryEx)
            {
                _logger.LogError(
                    retryEx,
                    "Retry handling failed. NACKing message for broker redelivery. DeliveryTag={DeliveryTag}",
                    eventArgs.DeliveryTag);

                _channel.BasicNack(eventArgs.DeliveryTag, false, requeue: true);
            }
        }
    }

    private static int GetRetryCount(IDictionary<string, object?>? headers)
    {
        if (headers is null || !headers.TryGetValue("x-retry-count", out var value) || value is null)
            return 0;

        return value switch
        {
            byte byteValue => byteValue,
            sbyte sbyteValue => sbyteValue,
            short shortValue => shortValue,
            ushort ushortValue => ushortValue,
            int intValue => intValue,
            uint uintValue => (int)uintValue,
            long longValue => (int)longValue,
            ulong ulongValue => (int)ulongValue,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsedBytes) => parsedBytes,
            string str when int.TryParse(str, out var parsedString) => parsedString,
            _ => 0
        };
    }

    private int GetRetryDelayMilliseconds(int currentRetryCount)
    {
        var initialDelayMs = Math.Max(1, _options.InitialRetryDelaySeconds) * 1000;
        var maxDelayMs = Math.Max(initialDelayMs, _options.MaxRetryDelaySeconds * 1000);

        var exponentialDelay = initialDelayMs * Math.Pow(2, Math.Max(0, currentRetryCount));
        return (int)Math.Min(exponentialDelay, maxDelayMs);
    }

    private static Document? CreateDocument(MinioObjectCreated? message)
    {
        var objectKey = GetObjectKey(message);
        if (string.IsNullOrWhiteSpace(objectKey))
            return null;

        // The object key is the source of truth for claim/document routing.
        // Example: claims/{claimId}/{documentType}/{fileName}
        var path = ParseObjectPath(objectKey);
        if (path is null)
            return null;

        return Document.Create(path.Value.ClaimId, path.Value.DocumentType, objectKey);
    }

    private static (Guid ClaimId, string DocumentType)? ParseObjectPath(string key)
    {
        var parts = key.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var claimsIndex = Array.FindIndex(parts, part =>
            string.Equals(part, "claims", StringComparison.OrdinalIgnoreCase));

        if (claimsIndex < 0 || parts.Length <= claimsIndex + 2)
            return null;

        if (!Guid.TryParse(parts[claimsIndex + 1], out var claimId))
            return null;

        var documentType = parts[claimsIndex + 2];
        if (string.IsNullOrWhiteSpace(documentType))
            return null;

        return (claimId, documentType);
    }

    private static string? GetObjectKey(MinioObjectCreated? message)
    {
        if (message is null)
            return null;

        if (!string.IsNullOrWhiteSpace(message.Key))
            return message.Key;

        var encodedKey = message.Records?
            .FirstOrDefault()?
            .S3?
            .Object?
            .Key;

        return string.IsNullOrWhiteSpace(encodedKey)
            ? null
            : WebUtility.UrlDecode(encodedKey);
    }
}