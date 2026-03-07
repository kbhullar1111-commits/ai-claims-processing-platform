using ClaimsService.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsService.API.Controllers;

/// <summary>
/// Handles claim submission endpoints.
/// </summary>
[ApiController]
[Route("claims")]
public class ClaimsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClaimsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Submits a new insurance claim.
    /// </summary>
    /// <param name="command">Claim details required to create a claim.</param>
    /// <returns>The created claim identifier.</returns>
    [HttpPost]
    public async Task<IActionResult> SubmitClaim(SubmitClaimCommand command)
    {
        var claimId = await _mediator.Send(command);

        return Ok(new { ClaimId = claimId });
    }
}
