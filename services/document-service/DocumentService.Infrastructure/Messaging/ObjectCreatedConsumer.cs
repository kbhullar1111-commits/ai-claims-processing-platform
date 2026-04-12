using BuildingBlocks.Contracts.Documents;
using DocumentService.Domain.Entities;
using DocumentService.Infrastructure.Persistence;
using MassTransit;
using System.Net;

namespace DocumentService.Infrastructure.Messaging;

public class ObjectCreatedConsumer :
    IConsumer<MinioObjectCreated>
{
    private readonly DocumentDbContext _db;

    public ObjectCreatedConsumer(DocumentDbContext db)
    {
        _db = db;
    }
    public async Task Consume(
        ConsumeContext<MinioObjectCreated> context)
    {
        var key = GetObjectKey(context.Message);

        if (string.IsNullOrWhiteSpace(key))
            return;

        // var exists = await _db.Documents
        //     .AnyAsync(x => x.ObjectKey == key);

        // if (exists)
        //     return;

        var path = ParseObjectPath(key);
        if (path is null)
            return;

        var document = Document.Create(
            path.Value.ClaimId,
            path.Value.DocumentType,
            key
        );

        _db.Documents.Add(document);
        await _db.SaveChangesAsync();

        await context.Publish(new DocumentUploaded(
            Guid.NewGuid(),
            path.Value.ClaimId,
            path.Value.DocumentType,
            DateTime.UtcNow
        ));
    }

    private static (Guid ClaimId, string DocumentType)? ParseObjectPath(string key)
    {
        var parts = key.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var claimsIndex = Array.FindIndex(parts, part =>
            string.Equals(part, "claims", StringComparison.OrdinalIgnoreCase));

        if (claimsIndex < 0 || parts.Length <= claimsIndex + 2)
            return null;

        if (!Guid.TryParse(parts[claimsIndex + 1], out var claimId))
            return null;

        var documentType = parts[claimsIndex + 2];
        if (string.IsNullOrWhiteSpace(documentType))
            return null;

        return (claimId, documentType);
    }

    private static string? GetObjectKey(MinioObjectCreated message)
    {
        if (!string.IsNullOrWhiteSpace(message.Key))
            return message.Key;

        var encodedKey = message.Records?
            .FirstOrDefault()?
            .S3?
            .Object?
            .Key;

        if (string.IsNullOrWhiteSpace(encodedKey))
            return null;

        return WebUtility.UrlDecode(encodedKey);
    }
}