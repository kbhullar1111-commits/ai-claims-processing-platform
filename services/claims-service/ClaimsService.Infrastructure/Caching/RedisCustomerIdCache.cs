using ClaimsService.Application.Interfaces;

namespace ClaimsService.Infrastructure.Caching;

public sealed class RedisCustomerIdCache : ICustomerIdCache
{
    private readonly RedisConnection _redis;

    public RedisCustomerIdCache(RedisConnection redis)
    {
        _redis = redis;
    }

    public async Task<Guid?> GetAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(email);

        var value = await _redis.Database.StringGetAsync(key);

        if (!value.HasValue)
        {
            return null;
        }

        return Guid.TryParse(value.ToString(), out var customerId)
            ? customerId
            : null;
    }

    public async Task SetAsync(
        string email,
        Guid customerId,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(email);

        await _redis.Database.StringSetAsync(
            key,
            customerId.ToString(),
            expiration);
    }

    public async Task RemoveAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(email);

        await _redis.Database.KeyDeleteAsync(key);
    }

    private static string BuildKey(string email)
    {
        return $"claims:customer-id:{email.Trim().ToLowerInvariant()}";
    }
}