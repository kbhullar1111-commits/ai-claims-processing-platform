using PolicyService.Domain.Entities;
using PolicyService.Domain.Enums;
using PolicyService.Application.Abstractions;
using PolicyService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace PolicyService.Infrastructure.Repositories;

public class PolicyTypeDefinitionRepository : IPolicyTypeDefinitionRepository
{
    private readonly PolicyDbContext _dbContext;

    public PolicyTypeDefinitionRepository(PolicyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PolicyTypeDefinition?> GetByIdAsync(Guid policyTypeDefinitionId, CancellationToken cancellationToken)
    {
        return await _dbContext.PolicyTypeDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(ptd => ptd.Id == policyTypeDefinitionId, cancellationToken);
    }

    public async Task<PolicyTypeDefinition?> GetApplicableAsync(
        string policyName,
        DateTime effectiveFrom,
        CancellationToken cancellationToken)
    {
        return await _dbContext.PolicyTypeDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                ptd => ptd.Name == policyName
                    && ptd.EffectiveFrom <= effectiveFrom
                    && (ptd.EffectiveTo == null || ptd.EffectiveTo > effectiveFrom),
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<PolicyTypeDefinition>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.PolicyTypeDefinitions.AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PolicyTypeDefinition policyTypeDefinition, CancellationToken cancellationToken)
    {
        await _dbContext.PolicyTypeDefinitions.AddAsync(policyTypeDefinition, cancellationToken);
    }
}