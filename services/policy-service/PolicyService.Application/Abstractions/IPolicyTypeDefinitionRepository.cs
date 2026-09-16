using PolicyService.Domain.Entities;

namespace PolicyService.Application.Abstractions;

public interface IPolicyTypeDefinitionRepository
{
    Task<PolicyTypeDefinition?> GetByIdAsync(Guid policyTypeDefinitionId, CancellationToken cancellationToken);
    Task<PolicyTypeDefinition?> GetApplicableAsync(string policyName, DateTime effectiveFrom, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PolicyTypeDefinition>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(PolicyTypeDefinition policyTypeDefinition, CancellationToken cancellationToken);
}