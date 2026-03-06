namespace BuildingBlocks.Contracts.Fraud;

public record FraudCheckCompleted(
    Guid ClaimId,
    decimal RiskScore,
    bool IsFraudulent,
    DateTime EvaluatedAt
);