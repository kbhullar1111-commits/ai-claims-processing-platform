namespace BuildingBlocks.Contracts.Payment;
public record ProcessPayment(
    Guid ClaimId,
    decimal Amount
);