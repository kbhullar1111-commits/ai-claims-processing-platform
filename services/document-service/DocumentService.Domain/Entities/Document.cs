namespace DocumentService.Domain.Entities;

public class Document
{
    public Guid Id { get; private set; }

    public Guid ClaimId { get; private set; }

    public string DocumentType { get; private set; } = default!;

    public string ObjectKey { get; private set; } = default!;

    public DateTime UploadedAt { get; private set; }

    private Document() { }

    public static Document Create(
        Guid claimId,
        string documentType,
        string objectKey)
    {
        return new Document
        {
            Id = Guid.NewGuid(),
            ClaimId = claimId,
            DocumentType = documentType,
            ObjectKey = objectKey,
            UploadedAt = DateTime.UtcNow
        };
    }
}