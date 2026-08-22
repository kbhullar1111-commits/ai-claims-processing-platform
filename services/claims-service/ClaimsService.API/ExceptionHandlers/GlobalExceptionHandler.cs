using Microsoft.AspNetCore.Diagnostics;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is TimeoutException)
        {
            _logger.LogError(
                exception,
                "Service call timed out.");

            httpContext.Response.StatusCode =
                StatusCodes.Status504GatewayTimeout;

            await httpContext.Response.WriteAsJsonAsync(
                new
                {
                    Message = "Service did not respond within the allowed time."
                },
                cancellationToken);

            return true;
        }

        return false;
    }
}