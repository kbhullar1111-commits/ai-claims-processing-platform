using ClaimsService.Application.Commands;
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
    private readonly ICustomerResolver _customerResolver;
    private readonly ILogger<ClaimsController> _logger;

    public ClaimsController(IMediator mediator, ICurrentUser currentUser, ICustomerResolver customerResolver, ILogger<ClaimsController> logger)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _customerResolver = customerResolver;
        _logger = logger;
    }

    /// <summary>
    /// Submits a new claim.
    /// </summary>
    /// <param name="request">Claim submission request.</param>
    /// <returns>Created claim identifier.</returns>
    [HttpPost]
    public async Task<IActionResult> SubmitClaim(SubmitClaimRequest request)
    {
        _logger.LogInformation("Submitting claim for user: {UserId}, email: {Email}, name: {Name}", _currentUser.UserId, _currentUser.Email, _currentUser.Name);
        var customer = _customerResolver.Resolve( _currentUser.UserId!);
        if(customer == null || string.IsNullOrEmpty(customer.CustomerId))
        {
            _logger.LogWarning(
                "Customer mapping not found. UserId={UserId}, Email={Email}",
                _currentUser.UserId,
                _currentUser.Email);
            return NotFound("Customer not found");
        }
        var command = new SubmitClaimCommand(
            Guid.Parse(customer.CustomerId),
            Guid.Parse(request.PolicyId),
            request.ClaimAmount);

        var claimId = await _mediator.Send(command);

        return Ok(new { ClaimId = claimId });
    }
}
