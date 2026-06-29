using Azure.Messaging.ServiceBus;
using BuildingBlocks.Contracts.Payment;
using Microsoft.Extensions.Logging;
using System.Text;

namespace PaymentService.Infrastructure;

public sealed class PaymentProcessedPublisher(
    ServiceBusClient client,
    PaymentMessagingOptions options,
    ILogger<PaymentProcessedPublisher> logger)
{
    private readonly string _topicName = options.PaymentProcessedTopic;

    public async Task PublishAsync(
        PaymentProcessed message,
        Guid? correlationId,
        Guid? conversationId,
        CancellationToken cancellationToken)
    {
        await using var sender = client.CreateSender(_topicName);

        var body = MassTransitInterop.SerializePaymentProcessedEnvelope(
            message,
            correlationId,
            conversationId);

        var outbound = new ServiceBusMessage(Encoding.UTF8.GetBytes(body))
        {
            ContentType = "application/vnd.masstransit+json",
            CorrelationId = (correlationId ?? message.ClaimId).ToString(),
            Subject = "PaymentProcessed"
        };

        outbound.ApplicationProperties["MT-MessageType"] = MassTransitInterop.PaymentProcessedMessageUrn;

        try
        {
            await sender.SendMessageAsync(outbound, cancellationToken);

            logger.LogInformation(
                "PaymentProcessed published. Topic={Topic}, ClaimId={ClaimId}, CorrelationId={CorrelationId}, ConversationId={ConversationId}, TransactionId={TransactionId}",
                _topicName,
                message.ClaimId,
                correlationId,
                conversationId,
                message.TransactionId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "PaymentProcessed publish failed. Topic='{Topic}', TopicLength={TopicLength}, ClaimId={ClaimId}, CorrelationId={CorrelationId}, ConversationId={ConversationId}, ContentType={ContentType}, Subject={Subject}",
                _topicName,
                _topicName.Length,
                message.ClaimId,
                correlationId,
                conversationId,
                outbound.ContentType,
                outbound.Subject);

            throw;
        }
    }

}
