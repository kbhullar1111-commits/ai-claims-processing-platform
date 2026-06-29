using ClaimsService.Application.Interfaces;

namespace ClaimsService.Infrastructure.Persistence;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly ClaimsDbContext _dbContext;

    public EfUnitOfWork(ClaimsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}