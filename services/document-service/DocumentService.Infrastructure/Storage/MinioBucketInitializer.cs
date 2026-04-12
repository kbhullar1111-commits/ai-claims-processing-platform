using Minio;
using Minio.DataModel.Args;
using Minio.DataModel.Notification;

namespace DocumentService.Infrastructure.Storage;

public static class MinioBucketInitializer
{
    public static async Task EnsureBucketConfigured(
        IMinioClient client,
        string bucketName,
        string queueArn)
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

        var current = await client.GetBucketNotificationsAsync(
            new GetBucketNotificationsArgs()
                .WithBucket(bucketName));

        var hasExpectedQueue = current?.QueueConfigs?.Any(q =>
            string.Equals(q.Queue, queueArn, StringComparison.OrdinalIgnoreCase) &&
            (q.Events?.Any(e =>
                string.Equals(e.Value, EventType.ObjectCreatedAll.Value, StringComparison.OrdinalIgnoreCase)) ?? false)) ?? false;

        if (hasExpectedQueue)
            return;

        var bucketNotification = current ?? new BucketNotification();
        bucketNotification.RemoveQueueByArn(new Arn(queueArn));

        var queueConfig = new QueueConfig(new Arn(queueArn));
        queueConfig.AddEvents([EventType.ObjectCreatedAll]);
        bucketNotification.AddQueue(queueConfig);

        await client.SetBucketNotificationsAsync(
            new SetBucketNotificationsArgs()
                .WithBucket(bucketName)
                .WithBucketNotificationConfiguration(bucketNotification));
    }
}