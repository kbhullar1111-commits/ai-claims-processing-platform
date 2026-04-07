using DocumentService.Application.DTOs;
using DocumentService.Application.Interfaces;
using MediatR;

namespace DocumentService.Application.Commands;

public class GenerateUploadUrlCommandHandler 
    : IRequestHandler<GenerateUploadUrlCommand, GenerateUploadUrlResult>
{
    private readonly IObjectStorage _storage;

    public GenerateUploadUrlCommandHandler(IObjectStorage storage)
    {
        _storage = storage;
    }

    public async Task<GenerateUploadUrlResult> Handle(
        GenerateUploadUrlCommand request,
        CancellationToken cancellationToken)
    {
        var objectKey = BuildObjectKey(
            request.ClaimId,
            request.DocumentType,
            request.FileName);

        var uploadUrl = await _storage.GenerateUploadUrl(
            objectKey,
            TimeSpan.FromMinutes(15));

        return new GenerateUploadUrlResult
        {
            UploadUrl = uploadUrl,
            ObjectKey = objectKey
        };
    }

    private static string BuildObjectKey(
        Guid claimId,
        string documentType,
        string fileName)
    {
        return $"claims/{claimId}/{documentType}/{fileName}";
    }
}