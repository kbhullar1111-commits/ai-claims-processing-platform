namespace DocumentService.Infrastructure.Messaging;

public class MinioObjectCreated
{
    public string? EventName { get; set; }

    public string? Key { get; set; }

    public List<MinioRecord>? Records { get; set; }
}

public class MinioRecord
{
    public S3Entity? S3 { get; set; }
}

public class S3Entity
{
    public BucketEntity? Bucket { get; set; }

    public ObjectEntity? Object { get; set; }
}

public class ObjectEntity
{
    public string? Key { get; set; }
}

public class BucketEntity
{
    public string? Name { get; set; }
}