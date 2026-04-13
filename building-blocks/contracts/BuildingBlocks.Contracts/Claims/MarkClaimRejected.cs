namespace BuildingBlocks.Contracts.Claims;

public record MarkClaimRejected(
    Guid ClaimId,
    string Reason
);