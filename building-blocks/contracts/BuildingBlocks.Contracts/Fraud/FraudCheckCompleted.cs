namespace BuildingBlocks.Contracts.Fraud;

public record FraudCheckCompleted(
    Guid ClaimId,
    decimal RiskScore,
    bool IsFraudulent,
    string? Reason,
    DateTime EvaluatedAt
);