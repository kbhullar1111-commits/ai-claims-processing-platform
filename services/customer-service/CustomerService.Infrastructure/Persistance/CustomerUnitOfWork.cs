using CustomerService.Application;

namespace CustomerService.Infrastructure.Persistence;

public class CustomerUnitOfWork : ICustomerUnitOfWork
{
    private readonly CustomerDbContext _dbContext;

    public CustomerUnitOfWork(CustomerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}