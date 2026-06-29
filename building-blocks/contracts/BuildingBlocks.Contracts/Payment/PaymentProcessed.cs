namespace BuildingBlocks.Contracts.Payment;

public record PaymentProcessed(
    Guid ClaimId,
    bool Success,
    string? FailureReason,
    string? TransactionId,
    DateTime ProcessedAt
);