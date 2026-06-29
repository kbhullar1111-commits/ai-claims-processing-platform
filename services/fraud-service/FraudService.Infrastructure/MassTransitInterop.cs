using BuildingBlocks.Contracts.Fraud;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FraudService.Infrastructure;

internal static class MassTransitInterop
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private static readonly JsonSerializerOptions OutgoingEnvelopeJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private const string FraudCheckCompletedUrn = "urn:message:BuildingBlocks.Contracts.Fraud:FraudCheckCompleted";

    public static string FraudCheckCompletedMessageUrn => FraudCheckCompletedUrn;

    public static bool TryDeserializeRunFraudCheck(
        string payload,
        out RunFraudCheck? command,
        out Guid? correlationId,
        out Guid? conversationId)
    {
        command = null;
        correlationId = null;
        conversationId = null;

        try
        {
            var envelope = JsonSerializer.Deserialize<IncomingEnvelope<RunFraudCheck>>(payload, JsonOptions);
            if (envelope?.Message is not null && envelope.Message.ClaimId != Guid.Empty)
            {
                command = envelope.Message;
                correlationId = envelope.CorrelationId;
                conversationId = envelope.ConversationId;
                return true;
            }
        }
        catch
        {
            // Not a MassTransit envelope — fall through to direct parse.
        }

        try
        {
            var direct = JsonSerializer.Deserialize<RunFraudCheck>(payload, JsonOptions);
            if (direct is not null && direct.ClaimId != Guid.Empty)
            {
                command = direct;
                correlationId = direct.ClaimId;
                conversationId = direct.ClaimId;
                return true;
            }
        }
        catch
        {
            // Not a valid payload.
        }

        return false;
    }

    public static string SerializeFraudCheckCompletedEnvelope(
        FraudCheckCompleted message,
        Guid? correlationId,
        Guid? conversationId)
    {
        var envelope = new OutgoingEnvelope<FraudCheckCompleted>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId,
            ConversationId = conversationId,
            MessageType = [FraudCheckCompletedUrn],
            Message = message,
            SentTime = DateTime.UtcNow,
            Headers = new Dictionary<string, object?>()
        };

        return JsonSerializer.Serialize(envelope, OutgoingEnvelopeJsonOptions);
    }

    private sealed class IncomingEnvelope<T>
    {
        public Guid? CorrelationId { get; set; }

        public Guid? ConversationId { get; set; }

        public T? Message { get; set; }
    }

    private sealed class OutgoingEnvelope<T>
    {
        public Guid MessageId { get; set; }

        public Guid? CorrelationId { get; set; }

        public Guid? ConversationId { get; set; }

        public string[] MessageType { get; set; } = [];

        public T? Message { get; set; }

        public DateTime SentTime { get; set; }

        public Dictionary<string, object?> Headers { get; set; } = [];
    }

}