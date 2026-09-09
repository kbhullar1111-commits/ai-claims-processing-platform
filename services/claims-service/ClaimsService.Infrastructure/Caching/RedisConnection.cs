using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.StackExchangeRedis;
using StackExchange.Redis;

namespace ClaimsService.Infrastructure.Caching;

public sealed class RedisConnection
{
    private readonly ILoggerFactory _loggerFactory;
    private ConnectionMultiplexer? _connection;

    public RedisConnection(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public IDatabase Database =>
        _connection?.GetDatabase()
        ?? throw new InvalidOperationException(
            "Redis connection has not been initialized.");

    public async Task ConnectAsync(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        if (_connection is not null)
            return;

        var configurationOptions = new ConfigurationOptions
        {
            EndPoints = { endpoint },
            Protocol = RedisProtocol.Resp3,
            LoggerFactory = _loggerFactory,

            // Learning environment: fail fast if Redis cannot be reached.
            // We will revisit these for production resilience.
            AbortOnConnectFail = true,
            BacklogPolicy = BacklogPolicy.FailFast
        };

        await configurationOptions
            .ConfigureForAzureWithTokenCredentialAsync(
                new DefaultAzureCredential());

        _connection =
            await ConnectionMultiplexer.ConnectAsync(
                configurationOptions);
    }
}