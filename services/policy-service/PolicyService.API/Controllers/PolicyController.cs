using Microsoft.AspNetCore.Mvc;
using PolicyService.API.Models;
using PolicyService.Application.Policies;

namespace PolicyService.API.Controllers;

[ApiController]
[Route("policies")]
public class PolicyController : ControllerBase
{
    private readonly CreatePolicyCommandHandler _createPolicyHandler;
    private readonly GetPolicyQueryHandler _getPolicyHandler;
    private readonly GetPoliciesQueryHandler _getPoliciesHandler;
    private readonly UpdatePolicyCommandHandler _updatePolicyHandler;

    public PolicyController(
        CreatePolicyCommandHandler createPolicyHandler,
        GetPolicyQueryHandler getPolicyHandler,
        GetPoliciesQueryHandler getPoliciesHandler,
        UpdatePolicyCommandHandler updatePolicyHandler)
    {
        _createPolicyHandler = createPolicyHandler;
        _getPolicyHandler = getPolicyHandler;
        _getPoliciesHandler = getPoliciesHandler;
        _updatePolicyHandler = updatePolicyHandler;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePolicy(
        CreatePolicyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePolicyCommand(
            request.CustomerId,
            request.PolicyName,
            request.PolicyTypeEffectiveFrom,
            request.EffectiveDate,
            request.ExpirationDate,
            new VehicleDetailsDto(
                request.RegistrationNumber,
                request.Model,
                request.VehicleType));

        var response = await _createPolicyHandler.HandleAsync(command, cancellationToken);

        return Created(
            $"/policies/{response.PolicyId}",
            response);
    }

    [HttpGet("{policyId:guid}")]
    public async Task<IActionResult> GetPolicy(
        Guid policyId,
        CancellationToken cancellationToken)
    {
        var query = new GetPolicyQuery(policyId);

        var response = await _getPolicyHandler.HandleAsync(query, cancellationToken);

        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetPolicies(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var query = new GetPoliciesQuery(customerId);

        var response = await _getPoliciesHandler.HandleAsync(query, cancellationToken);

        return Ok(response);
    }

    [HttpPut("{policyId:guid}")]
    public async Task<IActionResult> UpdatePolicy(
        Guid policyId,
        UpdatePolicyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePolicyCommand(
            policyId,
            request.Status
        );

        var response = await _updatePolicyHandler.HandleAsync(command, cancellationToken);

        return Ok(response);
    }

}