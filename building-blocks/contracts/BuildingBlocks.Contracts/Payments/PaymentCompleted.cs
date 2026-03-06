namespace BuildingBlocks.Contracts.Payments;

public record PaymentCompleted(
    Guid PaymentId,
    Guid ClaimId,
    decimal Amount,
    DateTime ProcessedAt
);