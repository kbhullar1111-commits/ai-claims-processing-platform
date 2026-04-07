using BuildingBlocks.Contracts.Documents;
using MassTransit;

namespace DocumentService.Infrastructure.Messaging;

public class ObjectCreatedConsumer :
    IConsumer<MinioObjectCreated>
{
    public async Task Consume(
        ConsumeContext<MinioObjectCreated> context)
    {
        var key = context.Message.ObjectKey;

        // claims/{claimId}/{documentType}/{file}
        var parts = key.Split('/');

        var claimId = Guid.Parse(parts[1]);
        var documentType = parts[2];

        await context.Publish(new DocumentUploaded
        {
            ClaimId = claimId,
            DocumentType = documentType,
            ObjectKey = key
        });
    }
}