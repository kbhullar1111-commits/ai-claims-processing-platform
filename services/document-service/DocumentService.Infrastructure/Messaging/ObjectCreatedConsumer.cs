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

        // MinIO emits object-created notifications to RabbitMQ. This service owns
        // a dedicated queue that receives those raw notifications.
        _channel.ExchangeDeclare(_options.MinioExchangeName, ExchangeType.Fanout, durable: true);
        _channel.QueueDeclare(_options.MinioObjectCreatedQueueName, durable: true, exclusive: false, autoDelete: false);
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
            _logger.LogError(ex, "Failed to persist MinIO object-created event to the document outbox.");
            // Requeue so the notification can be retried after transient failures.
            _channel.BasicNack(eventArgs.DeliveryTag, false, requeue: true);
        }
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