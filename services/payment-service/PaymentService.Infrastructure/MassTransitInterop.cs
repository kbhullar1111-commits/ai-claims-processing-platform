using BuildingBlocks.Contracts.Payment;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PaymentService.Infrastructure;

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

    private const string PaymentProcessedUrn = "urn:message:BuildingBlocks.Contracts.Payment:PaymentProcessed";

    public static string PaymentProcessedMessageUrn => PaymentProcessedUrn;

    public static bool TryDeserializeProcessPayment(
        string payload,
        out ProcessPayment? command,
        out Guid? correlationId,
        out Guid? conversationId)
    {
        command = null;
        correlationId = null;
        conversationId = null;

        try
        {
            var envelope = JsonSerializer.Deserialize<IncomingEnvelope<ProcessPayment>>(payload, JsonOptions);
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
            var direct = JsonSerializer.Deserialize<ProcessPayment>(payload, JsonOptions);
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
            // Not a direct ProcessPayment payload either.
        }

        return false;
    }

    public static string SerializePaymentProcessedEnvelope(
        PaymentProcessed message,
        Guid? correlationId,
        Guid? conversationId)
    {
        var envelope = new OutgoingEnvelope<PaymentProcessed>
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = correlationId,
            ConversationId = conversationId,
            MessageType = [PaymentProcessedUrn],
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
