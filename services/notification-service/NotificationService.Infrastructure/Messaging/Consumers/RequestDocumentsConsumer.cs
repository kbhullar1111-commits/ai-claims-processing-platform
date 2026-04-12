using MassTransit;
using BuildingBlocks.Contracts.Documents;
using Microsoft.Extensions.Logging;

namespace NotificationService.Infrastructure.Messaging.Consumers;

public class RequestDocumentsConsumer : IConsumer<RequestDocuments>
{
    private readonly ILogger<RequestDocumentsConsumer> _logger;

    public RequestDocumentsConsumer(ILogger<RequestDocumentsConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RequestDocuments> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "RequestDocuments received for ClaimId: {ClaimId} CustomerId: {CustomerId}",
            message.ClaimId,
            message.CustomerId);

        foreach (var doc in message.Documents)
        {
            _logger.LogInformation("Required document: {Document}", doc);
        }

        // Later:
        // send email
        // push notification
        // SMS
        // etc

        await Task.CompletedTask;
    }
}