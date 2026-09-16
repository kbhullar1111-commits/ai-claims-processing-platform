using PolicyService.Domain.Entities;
using PolicyService.Domain.Enums;
using PolicyService.Application.Abstractions;

namespace PolicyService.Application.PolicyTypeDefinitions;

public sealed class GetPolicyTypeDefinitionsQueryHandler
{
    private readonly IPolicyTypeDefinitionRepository _repository;

    public GetPolicyTypeDefinitionsQueryHandler(
        IPolicyTypeDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<PolicyTypeDefinitionsResponse>> HandleAsync(
        CancellationToken cancellationToken)
    {
        var definitions = await _repository.GetAllAsync(cancellationToken);

        return definitions
            .Select(x => new PolicyTypeDefinitionsResponse(
                x.Id,
                x.Type,
                x.Name,
                x.Coverages.Select(c => new CoverageDetailsDto(
                    c.Type,
                    c.CoverageLimit)
                ).ToList(),
                x.MaximumClaimAmount,
                x.AllowedVehicleTypes))
            .ToList();
    }
}