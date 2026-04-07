namespace DocumentService.API.RequestModels;

public class GenerateUploadUrlRequest
{
    public Guid ClaimId { get; set; }

    public string DocumentType { get; set; } = default!;

    public string FileName { get; set; } = default!;
}