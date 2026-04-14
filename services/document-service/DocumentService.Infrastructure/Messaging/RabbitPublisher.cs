using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;

namespace DocumentService.Infrastructure.Messaging;

public class RabbitPublisher
{
    private readonly RabbitMqOptions _options;
    private readonly IConnection _connection;

    public RabbitPublisher(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;

        var factory = new ConnectionFactory()
        {
            HostName = _options.Host,
            UserName = _options.Username,
            Password = _options.Password
        };

        _connection = factory.CreateConnection();
    }

    // This publisher intentionally uses raw RabbitMQ.Client so document-service stays independent of MassTransit.
    public Task PublishAsync(OutboxMessage msg)
    {
        using var channel = _connection.CreateModel();

        // The exchange is declared here as a safety net so the dispatcher can publish
        // even if the broker was restarted after startup.
        channel.ExchangeDeclare(_options.DocumentUploadedExchangeName, ExchangeType.Fanout, durable: true);

        var body = Encoding.UTF8.GetBytes(msg.Payload);
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.MessageId = msg.Id.ToString();
        properties.Type = msg.Type;

        // The payload is already serialized JSON from the outbox row.
        // RabbitMQ stores the message durably because both the exchange and message are persistent.
        channel.BasicPublish(
            exchange: _options.DocumentUploadedExchangeName,
            routingKey: "",
            basicProperties: properties,
            body: body);

        return Task.CompletedTask;
    }
}