namespace ClaimsService.Application.Interfaces;

public interface ICustomerIdCache
{
    Task<Guid?> GetAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        string email,
        Guid customerId,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        string email,
        CancellationToken cancellationToken = default);
}