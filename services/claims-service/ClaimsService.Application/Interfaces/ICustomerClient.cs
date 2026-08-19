using ClaimsService.Application.Models;

namespace ClaimsService.Application.Interfaces;

public interface ICustomerClient
{
    Task<CustomerContext?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken);
}