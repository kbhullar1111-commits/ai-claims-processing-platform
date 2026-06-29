using MediatR;
using DocumentService.Application.DTOs;

namespace DocumentService.Application.Commands;

public record GenerateUploadUrlCommand(
    Guid ClaimId,
    string DocumentType,
    string FileName
) : IRequest<GenerateUploadUrlResult>;