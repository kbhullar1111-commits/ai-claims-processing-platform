using ClaimsService.Application.Models;

namespace ClaimsService.Application.Interfaces;

public interface ICustomerClient
{
    Task<Guid?> GetCustomerIdByEmailAsync(
        string email,
        CancellationToken cancellationToken);
    Task<CustomerContext?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken);
}