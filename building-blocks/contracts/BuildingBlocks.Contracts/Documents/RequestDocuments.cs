namespace BuildingBlocks.Contracts.Documents;
public record RequestDocuments(
    Guid ClaimId,
    Guid CustomerId,
    IEnumerable<string> Documents
);