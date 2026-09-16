using PolicyService.Domain.Entities;
using PolicyService.Domain.Enums;
using PolicyService.Application.Abstractions;
using PolicyService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace PolicyService.Infrastructure.Repositories;

public class PolicyRepository : IPolicyRepository
{
    private readonly PolicyDbContext _dbContext;

    public PolicyRepository(PolicyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Policy?> GetByIdAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Policies.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == policyId, cancellationToken);
    }

    public async Task<Policy?> GetByIdForUpdateAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Policies
            .FirstOrDefaultAsync(p => p.Id == policyId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Policy>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken)
    {
        return await _dbContext.Policies.AsNoTracking()
            .Where(p => p.CustomerId == customerId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Policy policy, CancellationToken cancellationToken = default)
    {
        await _dbContext.Policies.AddAsync(policy, cancellationToken);
    }

}