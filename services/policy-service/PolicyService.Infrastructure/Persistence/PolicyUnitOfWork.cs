using PolicyService.Application.Abstractions;

namespace PolicyService.Infrastructure.Persistence;

public class PolicyUnitOfWork : IPolicyUnitOfWork
{
    private readonly PolicyDbContext _dbContext;

    public PolicyUnitOfWork(PolicyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}