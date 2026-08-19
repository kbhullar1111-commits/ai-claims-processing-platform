using ClaimsService.Application.Commands;
using ClaimsService.Application.Queries;
using ClaimsService.Application.Models;
using ClaimsService.Application.Interfaces;
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
    private readonly ICurrentUser _currentUser;
    private readonly ICustomerClient _customerClient;
    private readonly ILogger<ClaimsController> _logger;

    public ClaimsController(IMediator mediator, ICurrentUser currentUser, ICustomerClient customerClient, ILogger<ClaimsController> logger)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _customerClient = customerClient;
        _logger = logger;
    }

    /// <summary>
    /// Submits a new claim.
    /// </summary>
    /// <param name="request">Claim submission request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created claim identifier.</returns>
    [HttpPost]
    public async Task<IActionResult> SubmitClaim(SubmitClaimRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Submitting claim for user: {UserId}, email: {Email}, name: {Name}", _currentUser.UserId, _currentUser.Email, _currentUser.Name);
        var customer = await _customerClient.GetByEmailAsync(
        _currentUser.Email!, cancellationToken);

        if (customer == null)
        {
            _logger.LogWarning("Customer not found for email: {Email}", _currentUser.Email);
            return NotFound(new { Message = "Customer not found." });
        }


        var command = new SubmitClaimCommand(
            customer.CustomerId,
            Guid.Parse(request.PolicyId),
            request.ClaimAmount);

        var claimId = await _mediator.Send(command);

        return Ok(new { ClaimId = claimId });
    }

    [HttpGet]
    public async Task<IActionResult> GetMyClaims()
    {
        var query = new GetMyClaimsQuery();
        var claims = await _mediator.Send(query);
        return Ok(claims);
    }

    [HttpGet("{claimId:guid}")]
    public async Task<IActionResult> GetClaimDetails(Guid claimId)
    {
        var query = new GetClaimDetailsQuery(claimId);
        var claim = await _mediator.Send(query);
        if (claim == null)
        {
            return NotFound();
        }
        return Ok(claim);
    }

    [HttpGet("{claimId:guid}/history")]
    public async Task<IActionResult> GetClaimHistory(Guid claimId)
    {
        var history = await _mediator.Send(
            new GetClaimHistoryQuery(claimId));

        if (history == null)
        {
            return NotFound();
        }

        return Ok(history);
    }

}
