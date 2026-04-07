using DocumentService.Application.Interfaces;
using Minio;
using Minio.DataModel.Args;

namespace DocumentService.Infrastructure.Storage;

public class MinioObjectStorage : IObjectStorage
{
    private readonly IMinioClient _client;
    private readonly string _bucketName;

    public MinioObjectStorage(
        IMinioClient client,
        string bucketName)
    {
        _client = client;
        _bucketName = bucketName;
    }

    public async Task<string> GenerateUploadUrl(
        string objectKey,
        TimeSpan expiry)
    {
        var args = new PresignedPutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectKey)
            .WithExpiry((int)expiry.TotalSeconds);

        return await _client.PresignedPutObjectAsync(args);
    }
}