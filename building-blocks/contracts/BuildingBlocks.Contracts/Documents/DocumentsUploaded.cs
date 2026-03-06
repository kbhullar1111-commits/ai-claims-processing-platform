namespace BuildingBlocks.Contracts.Documents;

public record DocumentsUploaded(
    Guid DocumentId,
    Guid ClaimId,
    string DocumentType,
    DateTime UploadedAt
);