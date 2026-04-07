namespace BuildingBlocks.Contracts.Documents;

public record DocumentUploaded(
    Guid DocumentId,
    Guid ClaimId,
    string DocumentType,
    DateTime UploadedAt
);