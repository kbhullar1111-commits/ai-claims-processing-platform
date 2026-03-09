using NotificationService.Application.Interfaces;

namespace NotificationService.Infrastructure.Persistence;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly NotificationDbContext _dbContext;

    public EfUnitOfWork(NotificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}