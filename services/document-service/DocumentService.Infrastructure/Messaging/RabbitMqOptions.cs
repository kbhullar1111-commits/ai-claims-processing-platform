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

    public string MinioDeadLetterExchangeName { get; set; } = "minio-dlx";

    public string MinioObjectCreatedDeadLetterQueueName { get; set; } = "minio-object-created-dlq";

    public string MinioDeadLetterRoutingKey { get; set; } = "dlq";

    public string MinioRetryExchangeName { get; set; } = "minio-retry";

    public string MinioObjectCreatedRetryQueueName { get; set; } = "minio-object-created-retry";

    public string MinioRetryRoutingKey { get; set; } = "retry";

    public int MaxRetryAttempts { get; set; } = 3;

    public int InitialRetryDelaySeconds { get; set; } = 5;

    public int MaxRetryDelaySeconds { get; set; } = 300;

    public bool ThrowTestException { get; set; } = false;
}