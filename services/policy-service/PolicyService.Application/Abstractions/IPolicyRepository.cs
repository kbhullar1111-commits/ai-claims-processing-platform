using PolicyService.Domain.Entities;

namespace PolicyService.Application.Abstractions;

public interface IPolicyRepository
{
    Task<Policy?> GetByIdAsync(Guid policyId, CancellationToken cancellationToken);
    Task<Policy?> GetByIdForUpdateAsync(Guid policyId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Policy>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken);
    Task AddAsync(Policy policy, CancellationToken cancellationToken);
}