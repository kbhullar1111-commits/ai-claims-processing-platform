using DocumentService.Application.Commands;
using DocumentService.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using DocumentService.API.RequestModels;

namespace DocumentService.API.Controllers;

[ApiController]
[Route("documents")]
public class DocumentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DocumentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("upload-url")]
    public async Task<ActionResult<GenerateUploadUrlResult>> GenerateUploadUrl(
        GenerateUploadUrlRequest request)
    {
        var result = await _mediator.Send(
            new GenerateUploadUrlCommand(
                request.ClaimId,
                request.DocumentType,
                request.FileName));

        return Ok(result);
    }
}