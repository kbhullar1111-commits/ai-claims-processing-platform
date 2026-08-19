using CustomerService.Domain.Entities;

namespace CustomerService.Application;

public interface ICustomerRepository
{
    Task AddAsync(
        Customer customer,
        CancellationToken cancellationToken);

    Task<Customer?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<Customer?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken);

    Task<Customer?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Customer>> GetAllAsync(
        CancellationToken cancellationToken);
}