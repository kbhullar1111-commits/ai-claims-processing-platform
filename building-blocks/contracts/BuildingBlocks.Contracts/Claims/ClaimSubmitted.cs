namespace BuildingBlocks.Contracts.Claims;

public record ClaimSubmitted(
    Guid ClaimId,
    Guid CustomerId,
    Guid PolicyId,
    decimal ClaimAmount,
    DateTime SubmittedAt,
    IEnumerable<string> RequiredDocuments
);