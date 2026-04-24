using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using DocumentService.Application.Interfaces;

public class AzureBlobObjectStorage : IObjectStorage
{
    private readonly BlobContainerClient _container;

    public AzureBlobObjectStorage(
        string connectionString,
        string containerName)
    {
        var serviceClient =
            new BlobServiceClient(connectionString);

        _container =
            serviceClient.GetBlobContainerClient(containerName);
    }

    public async Task<string> GenerateUploadUrl(
        string objectKey,
        TimeSpan expiry)
    {
        var blobClient = _container.GetBlobClient(objectKey);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _container.Name,
            BlobName = objectKey,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
        };

        sasBuilder.SetPermissions(
            BlobSasPermissions.Write);

        return blobClient.GenerateSasUri(sasBuilder)
            .ToString();
    }
}