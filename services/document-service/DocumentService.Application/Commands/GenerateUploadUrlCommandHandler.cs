using DocumentService.Application.DTOs;
using DocumentService.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DocumentService.Application.Commands;

public class GenerateUploadUrlCommandHandler 
    : IRequestHandler<GenerateUploadUrlCommand, GenerateUploadUrlResult>
{
    private readonly IObjectStorage _storage;
    private readonly ILogger<GenerateUploadUrlCommandHandler> _logger;

    public GenerateUploadUrlCommandHandler(
        IObjectStorage storage,
        ILogger<GenerateUploadUrlCommandHandler> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<GenerateUploadUrlResult> Handle(
        GenerateUploadUrlCommand request,
        CancellationToken cancellationToken)
    {
        var objectKey = BuildObjectKey(
            request.ClaimId,
            request.DocumentType,
            request.FileName);

        _logger.LogInformation(
            "Generating upload URL. ClaimId={ClaimId}, DocumentType={DocumentType}",
            request.ClaimId,
            request.DocumentType);

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