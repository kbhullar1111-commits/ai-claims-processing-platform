namespace DocumentService.Infrastructure.Storage;

public class ObjectStorageOptions
{
    public string Endpoint { get; set; } = default!;

    public string AccessKey { get; set; } = default!;

    public string SecretKey { get; set; } = default!;

    public string Bucket { get; set; } = default!;

    public bool UseSsl { get; set; }
}