using BuildingBlocks.Contracts.Documents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ClaimsService.Infrastructure.Messaging;

public class DocumentUploadedBridgeConsumer : IConsumer<DocumentUploadedRawMessage>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DocumentUploadedBridgeConsumer> _logger;

    public DocumentUploadedBridgeConsumer(
        IPublishEndpoint publishEndpoint,
        ILogger<DocumentUploadedBridgeConsumer> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DocumentUploadedRawMessage> context)
    {
        var message = context.Message;

        if (message.DocumentId == Guid.Empty ||
            message.ClaimId == Guid.Empty ||
            string.IsNullOrWhiteSpace(message.DocumentType))
        {
            _logger.LogWarning(
                "Skipping invalid raw DocumentUploaded payload. DocumentId={DocumentId}, ClaimId={ClaimId}, DocumentType={DocumentType}",
                message.DocumentId,
                message.ClaimId,
                message.DocumentType);
            return;
        }

        // The bridge isolates raw RabbitMQ payloads from the saga. From here on,
        // the rest of claims-service works with the normal typed contract again.
        await _publishEndpoint.Publish(new DocumentUploaded(
            message.DocumentId,
            message.ClaimId,
            message.DocumentType,
            message.UploadedAt
        ), context.CancellationToken);
    }
}