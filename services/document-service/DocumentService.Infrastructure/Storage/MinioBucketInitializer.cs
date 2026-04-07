using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;

namespace DocumentService.Infrastructure.Storage;

public static class MinioBucketInitializer
{
    public static async Task EnsureBucketExists(
        IMinioClient client,
        string bucketName)
    {
        var exists = await client.BucketExistsAsync(
            new BucketExistsArgs()
                .WithBucket(bucketName));

        if (!exists)
        {
            await client.MakeBucketAsync(
                new MakeBucketArgs()
                    .WithBucket(bucketName));
        }

        await ConfigureNotifications(client, bucketName);
    }

    private static async Task ConfigureNotifications(
        IMinioClient client,
        string bucketName)
    {
        var config = new NotificationConfiguration();

        config.QueueConfigurationList.Add(
            new QueueConfiguration
            {
                Queue = "arn:minio:sqs::primary:amqp",
                Events = new List<string>
                {
                    "s3:ObjectCreated:*"
                }
            });

        await client.SetBucketNotificationAsync(
            new SetBucketNotificationArgs()
                .WithBucket(bucketName)
                .WithNotificationConfiguration(config));
    }
}