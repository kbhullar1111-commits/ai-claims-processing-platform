using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using OpenTelemetry;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Senders;
using NotificationService.Infrastructure.Observability.Metrics;

namespace NotificationService.Infrastructure.Workers;

public class NotificationDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationDispatcher> _logger;
    private readonly NotificationDispatcherOptions _options;
    private readonly INotificationMetrics _notificationMetrics;

    public NotificationDispatcher(
        IServiceScopeFactory scopeFactory,
        IOptions<NotificationDispatcherOptions> options,
        ILogger<NotificationDispatcher> logger,
        INotificationMetrics notificationMetrics)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
        _notificationMetrics = notificationMetrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification dispatcher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                IEnumerable<INotificationSender> senders = scope.ServiceProvider.GetServices<INotificationSender>();
                using (SuppressInstrumentationScope.Begin())
                {
                    var notifications = await repository.GetPendingAsync(_options.BatchSize, stoppingToken);
                    var senderMap = senders.ToDictionary(s => s.Channel);

                    foreach (var notification in notifications)
                    {
                        await ProcessNotification(notification, senderMap, stoppingToken);
                    }

                    await unitOfWork.CommitAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing notifications");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessNotification(Notification notification, IDictionary<NotificationChannel, INotificationSender> senderMap, CancellationToken cancellationToken)
    {
        try
        {
            senderMap.TryGetValue(notification.Channel, out var sender);

            if (sender == null)
            {
                notification.Parameters.TryGetValue("ClaimId", out var claimId);
                notification.MarkFailed();
                _notificationMetrics.NotificationFailed(notification.Channel.ToString());

                _logger.LogWarning(
                    "No sender found; notification marked failed. NotificationId={NotificationId}, EventId={EventId}, ClaimId={ClaimId}, CustomerId={CustomerId}, Channel={Channel}",
                    notification.NotificationId,
                    notification.EventId,
                    claimId,
                    notification.CustomerId,
                    notification.Channel);

                return;
            }

            var stopwatch = Stopwatch.StartNew();
            await sender.SendAsync(notification, cancellationToken);
            stopwatch.Stop();
            _notificationMetrics.NotificationSendDuration(
                    notification.Channel.ToString(),stopwatch.Elapsed.TotalSeconds);

            notification.MarkSent();
            _notificationMetrics.NotificationSent(notification.Channel.ToString());

        }
        catch (Exception ex)
        {
            notification.Parameters.TryGetValue("ClaimId", out var claimId);

            if (notification.RetryCount + 1 >= _options.MaxRetryAttempts)
            {
                notification.MarkFailed();
                _notificationMetrics.NotificationFailed(notification.Channel.ToString());

                _logger.LogError(
                    ex,
                    "Notification marked failed. NotificationId={NotificationId}, EventId={EventId}, ClaimId={ClaimId}, CustomerId={CustomerId}, RetryCount={RetryCount}",
                    notification.NotificationId,
                    notification.EventId,
                    claimId,
                    notification.CustomerId,
                    notification.RetryCount);
                return;
            }
            var delayMinutes = (int)Math.Min(Math.Pow(2, notification.RetryCount), 60);
            notification.ScheduleRetry(delayMinutes);
            _notificationMetrics.NotificationRetried(notification.Channel.ToString());
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


}   