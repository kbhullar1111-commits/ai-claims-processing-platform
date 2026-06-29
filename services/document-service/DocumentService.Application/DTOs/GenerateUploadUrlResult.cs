namespace DocumentService.Application.DTOs;

public class GenerateUploadUrlResult
{
    public string UploadUrl { get; init; } = default!;

    public string ObjectKey { get; init; } = default!;
}