

namespace CustomerService.Application;

public interface ICustomerUnitOfWork
{
    Task CommitAsync(
        CancellationToken cancellationToken);
}