using ClaimsService.Application.Commands;
using ClaimsService.Application.Queries;
using ClaimsService.Application.Models;
using ClaimsService.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsService.API.Controllers;

[ApiController]
[Route("claims")]
public class ClaimsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;
    private readonly ICustomerClient _customerClient;
    private readonly IPolicyClient _policyClient;
    private readonly ILogger<ClaimsController> _logger;

    public ClaimsController(
        IMediator mediator,
        ICurrentUser currentUser,
        ICustomerClient customerClient,
        IPolicyClient policyClient,
        ILogger<ClaimsController> logger)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _customerClient = customerClient;
        _policyClient = policyClient;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> SubmitClaim(
        SubmitClaimRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUser.Email))
        {
            _logger.LogWarning("Current user email is null or empty.");
            return BadRequest(new { Message = "User email is required." });
        }

        var customerId = await _customerClient.GetCustomerIdByEmailAsync(
            _currentUser.Email,
            cancellationToken);

        if (customerId is not Guid resolvedCustomerId)
        {
            _logger.LogWarning("Customer not found for email: {Email}", _currentUser.Email);
            return NotFound(new { Message = "Customer not found." });
        }

        var policyValidation = await _policyClient.ValidateClaimAsync(
            new ValidateClaimRequest(
                resolvedCustomerId,
                request.VehicleRegistrationNumber,
                request.IncidentType,
                request.ClaimAmount,
                request.IncidentDate),
            cancellationToken);

        if (!policyValidation.Eligible)
        {
            return BadRequest(new
            {
                Message = policyValidation.Reason
                    ?? "The claim is not eligible under the policy."
            });
        }

        if (policyValidation.PolicyId is not Guid policyId)
        {
            return BadRequest(new
            {
                Message = "Policy validation succeeded but no policy ID was returned."
            });
        }

        var command = new SubmitClaimCommand(
            resolvedCustomerId,
            policyId,
            request.ClaimAmount);

        var claimId = await _mediator.Send(command, cancellationToken);

        return Ok(new { ClaimId = claimId });
    }

    [HttpGet]
    public async Task<IActionResult> GetMyClaims()
    {
        var claims = await _mediator.Send(new GetMyClaimsQuery());
        return Ok(claims);
    }

    [HttpGet("{claimId:guid}")]
    public async Task<IActionResult> GetClaimDetails(Guid claimId)
    {
        var claim = await _mediator.Send(new GetClaimDetailsQuery(claimId));
        return claim == null ? NotFound() : Ok(claim);
    }

    [HttpGet("{claimId:guid}/history")]
    public async Task<IActionResult> GetClaimHistory(Guid claimId)
    {
        var history = await _mediator.Send(new GetClaimHistoryQuery(claimId));
        return history == null ? NotFound() : Ok(history);
    }
}
