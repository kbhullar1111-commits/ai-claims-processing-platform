using Microsoft.AspNetCore.Mvc;
using PolicyService.API.Models;
using PolicyService.Application.PolicyTypeDefinitions;

namespace PolicyService.API.Controllers;

[ApiController]
[Route("policy-types")]
public class PolicyTypeController : ControllerBase
{
    private readonly GetPolicyTypeDefQueryHandler _getPolicyTypeDefHandler;
    private readonly GetPolicyTypeDefinitionsQueryHandler _getPolicyTypeDefsHandler;
    private readonly CreatePolicyTypeDefinitionHandler _createPolicyTypeDefHandler;


    public PolicyTypeController(
        GetPolicyTypeDefQueryHandler getPolicyTypeDefHandler,
        GetPolicyTypeDefinitionsQueryHandler getPolicyTypeDefsHandler,
        CreatePolicyTypeDefinitionHandler createPolicyTypeDefHandler)
    {
        _getPolicyTypeDefHandler = getPolicyTypeDefHandler;
        _getPolicyTypeDefsHandler = getPolicyTypeDefsHandler;
        _createPolicyTypeDefHandler = createPolicyTypeDefHandler;
    }

    [HttpGet("{policyTypeDefinitionId:guid}")]
    public async Task<IActionResult> GetPolicyType(
        Guid policyTypeDefinitionId,
        CancellationToken cancellationToken)
    {
        var query = new GetPolicyTypeDefQuery(policyTypeDefinitionId);

        var response = await _getPolicyTypeDefHandler.Handle(query, cancellationToken);

        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetPolicyTypes(
        CancellationToken cancellationToken)
    {
        var response = await _getPolicyTypeDefsHandler.HandleAsync(cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePolicyType(
        CreatePolicyTypeDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePolicyTypeDefinitionCommand(
            request.Type,
            request.Name,
            request.EffectiveFrom,
            request.EffectiveTo,
            request.Coverages,
            request.MaximumClaimAmount,
            request.AllowedVehicleTypes);

        var response = await _createPolicyTypeDefHandler.HandleAsync(command, cancellationToken);

        return Created(
            $"/policy-types/{response.PolicyTypeDefinitionId}",
            response);
    }

}
