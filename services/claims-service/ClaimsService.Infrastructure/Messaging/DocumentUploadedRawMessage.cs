namespace ClaimsService.Infrastructure.Messaging;

public class DocumentUploadedRawMessage
{
    public Guid DocumentId { get; set; }

    public Guid ClaimId { get; set; }

    public string? DocumentType { get; set; }

    public DateTime UploadedAt { get; set; }
}