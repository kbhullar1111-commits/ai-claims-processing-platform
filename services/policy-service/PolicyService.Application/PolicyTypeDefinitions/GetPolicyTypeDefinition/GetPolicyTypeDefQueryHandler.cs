using PolicyService.Domain.Entities;
using PolicyService.Domain.Enums;
using PolicyService.Application.Abstractions;

namespace PolicyService.Application.PolicyTypeDefinitions;

public sealed class GetPolicyTypeDefQueryHandler
{
    private readonly IPolicyTypeDefinitionRepository _repository;

    public GetPolicyTypeDefQueryHandler(IPolicyTypeDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetPolicyTypeDefResponse> Handle(GetPolicyTypeDefQuery query, CancellationToken cancellationToken)
    {
        var policyTypeDefinition = await _repository.GetByIdAsync(query.PolicyTypeDefinitionId, cancellationToken);

        if (policyTypeDefinition == null)
        {
            throw new KeyNotFoundException($"Policy type definition with ID {query.PolicyTypeDefinitionId} not found.");
        }

        var coverageDetails = policyTypeDefinition.Coverages.Select(c => new CoverageDetailsDto(c.Type, c.CoverageLimit)).ToList();

        return new GetPolicyTypeDefResponse(
            policyTypeDefinition.Id,
            policyTypeDefinition.Name,
            policyTypeDefinition.Type,
            policyTypeDefinition.MaximumClaimAmount,
            coverageDetails,
            policyTypeDefinition.AllowedVehicleTypes);
    }
}