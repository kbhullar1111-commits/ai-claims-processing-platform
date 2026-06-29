using BuildingBlocks.Contracts.Claims;
using BuildingBlocks.Contracts.Documents;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NotificationService.Infrastructure.Messaging;

internal static class MassTransitInterop
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public static bool TryDeserializeClaimSubmitted(
        string payload,
        out ClaimSubmitted? message,
        out Guid? envelopeMessageId)
    {
        message = null;
        envelopeMessageId = null;

        try
        {
            var envelope = JsonSerializer.Deserialize<IncomingEnvelope<ClaimSubmitted>>(payload, JsonOptions);
            if (envelope?.Message is not null && envelope.Message.ClaimId != Guid.Empty)
            {
                message = envelope.Message;
                envelopeMessageId = envelope.MessageId;
                return true;
            }
        }
        catch
        {
            // Not a MassTransit envelope.
        }

        try
        {
            var direct = JsonSerializer.Deserialize<ClaimSubmitted>(payload, JsonOptions);
            if (direct is not null && direct.ClaimId != Guid.Empty)
            {
                message = direct;
                envelopeMessageId = direct.ClaimId;
                return true;
            }
        }
        catch
        {
            // Not a direct ClaimSubmitted payload.
        }

        return false;
    }

    public static bool TryDeserializeRequestDocuments(
        string payload,
        out RequestDocuments? message,
        out Guid? envelopeMessageId)
    {
        message = null;
        envelopeMessageId = null;

        try
        {
            var envelope = JsonSerializer.Deserialize<IncomingEnvelope<RequestDocuments>>(payload, JsonOptions);
            if (envelope?.Message is not null && envelope.Message.ClaimId != Guid.Empty)
            {
                message = envelope.Message;
                envelopeMessageId = envelope.MessageId;
                return true;
            }
        }
        catch
        {
            // Not a MassTransit envelope.
        }

        try
        {
            var direct = JsonSerializer.Deserialize<RequestDocuments>(payload, JsonOptions);
            if (direct is not null && direct.ClaimId != Guid.Empty)
            {
                message = direct;
                envelopeMessageId = direct.ClaimId;
                return true;
            }
        }
        catch
        {
            // Not a direct RequestDocuments payload.
        }

        return false;
    }

    private sealed class IncomingEnvelope<T>
    {
        public Guid? MessageId { get; set; }

        public T? Message { get; set; }
    }
}
