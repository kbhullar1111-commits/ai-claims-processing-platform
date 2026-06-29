namespace DocumentService.Infrastructure.Storage;

public class ObjectStorageOptions
{
    public string Endpoint { get; set; } = default!;

    /// <summary>
    /// The externally reachable endpoint used in presigned URLs.
    /// Defaults to Endpoint when not set.
    /// </summary>
    public string? PublicEndpoint { get; set; }

    public string AccessKey { get; set; } = default!;

    public string SecretKey { get; set; } = default!;

    public string Bucket { get; set; } = default!;

    public bool UseSsl { get; set; }
}