using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Interfaces;
using NotificationService.Infrastructure.Senders;

namespace NotificationService.Infrastructure.Workers;

public class NotificationDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationDispatcher> _logger;
    private readonly NotificationDispatcherOptions _options;

    public NotificationDispatcher(
        IServiceScopeFactory scopeFactory,
        IOptions<NotificationDispatcherOptions> options,
        ILogger<NotificationDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification dispatcher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNotifications(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing notifications");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessNotifications(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IEnumerable<INotificationSender> senders = scope.ServiceProvider.GetServices<INotificationSender>();
            

        var notifications = await repository.GetPendingAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            try
            {
                var sender = senders.FirstOrDefault(s => s.Channel == notification.Channel);

                if (sender == null)
                {
                    notification.Parameters.TryGetValue("ClaimId", out var claimId);

                    _logger.LogWarning(
                        "No sender found. NotificationId={NotificationId}, EventId={EventId}, ClaimId={ClaimId}, CustomerId={CustomerId}, Channel={Channel}",
                        notification.NotificationId,
                        notification.EventId,
                        claimId,
                        notification.CustomerId,
                        notification.Channel);

                    continue;
                }

                await sender.SendAsync(notification, cancellationToken);

                notification.MarkSent();
            }
            catch (Exception ex)
            {
                notification.Parameters.TryGetValue("ClaimId", out var claimId);

                if (notification.RetryCount + 1 >= _options.MaxRetryAttempts)
                {
                    notification.MarkFailed();
                    _logger.LogError(
                        ex,
                        "Notification marked failed. NotificationId={NotificationId}, EventId={EventId}, ClaimId={ClaimId}, CustomerId={CustomerId}, RetryCount={RetryCount}",
                        notification.NotificationId,
                        notification.EventId,
                        claimId,
                        notification.CustomerId,
                        notification.RetryCount);
                    continue;
                }

                notification.ScheduleRetry(_options.RetryDelayMinutes);
                _logger.LogWarning(
                    ex,
                    "Notification failed; scheduled retry. NotificationId={NotificationId}, EventId={EventId}, ClaimId={ClaimId}, CustomerId={CustomerId}, RetryCount={RetryCount}",
                    notification.NotificationId,
                    notification.EventId,
                    claimId,
                    notification.CustomerId,
                    notification.RetryCount);
            }
        }

        await unitOfWork.CommitAsync(cancellationToken);
    }
}   