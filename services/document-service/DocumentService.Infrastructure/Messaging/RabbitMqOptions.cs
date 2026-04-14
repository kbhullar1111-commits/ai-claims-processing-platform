namespace DocumentService.Infrastructure.Messaging;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = "localhost";

    public string Username { get; set; } = "claimsuser";

    public string Password { get; set; } = "claimspassword";

    public string MinioObjectCreatedQueueName { get; set; } = "minio-object-created";

    public string MinioExchangeName { get; set; } = "minio";

    public string MinioNotificationArn { get; set; } = "arn:minio:sqs::PRIMARY:amqp";

    public string DocumentUploadedExchangeName { get; set; } = "document-uploaded";
}