namespace DocumentService.Application.Interfaces;

public interface IObjectStorage
{
    Task<string> GenerateUploadUrl(
        string objectKey,
        TimeSpan expiry);
}