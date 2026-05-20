using Azure.Messaging.ServiceBus;
using BuildingBlocks.Contracts.Fraud;
using Microsoft.Extensions.Logging;
using System.Text;

namespace FraudService.Infrastructure;

public sealed class FraudCheckPublisher(
    ServiceBusClient client,
    FraudCheckMessagingOptions options,
    ILogger<FraudCheckPublisher> logger)
{
    private readonly string _topicName = options.FraudCheckCompletedTopic;

    public async Task PublishAsync(
        FraudCheckCompleted message,
        Guid? correlationId,
        Guid? conversationId,
        CancellationToken cancellationToken)
    {
        await using var sender = client.CreateSender(_topicName);

        var body = MassTransitInterop.SerializeFraudCheckCompletedEnvelope(
            message,
            correlationId,
            conversationId);

        var outbound = new ServiceBusMessage(Encoding.UTF8.GetBytes(body))
        {
            ContentType = "application/vnd.masstransit+json",
            CorrelationId = (correlationId ?? message.ClaimId).ToString(),
            Subject = "FraudCheckCompleted"
        };

        outbound.ApplicationProperties["MT-MessageType"] = MassTransitInterop.FraudCheckCompletedMessageUrn;

        try
        {
            await sender.SendMessageAsync(outbound, cancellationToken);

            logger.LogInformation(
                "FraudCheckCompleted published. Topic={Topic}, ClaimId={ClaimId}, CorrelationId={CorrelationId}, ConversationId={ConversationId}",
                _topicName,
                message.ClaimId,
                correlationId,
                conversationId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "FraudCheckCompleted publish failed. Topic='{Topic}', TopicLength={TopicLength}, ClaimId={ClaimId}, CorrelationId={CorrelationId}, ConversationId={ConversationId}, ContentType={ContentType}, Subject={Subject}",
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