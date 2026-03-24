using System.Diagnostics.Metrics;
using NotificationService.Infrastructure.Observability.Constants;
using NotificationService.Application.Interfaces;

namespace NotificationService.Infrastructure.Observability.Metrics;

public class NotificationMetrics : INotificationMetrics
{
    private const string MeterName = TelemetryConstants.MeterName;
    private static readonly Meter Meter = new(MeterName);

    private readonly Counter<long> _notificationsCreated;
    private readonly Counter<long> _notificationsSent;
    private readonly Counter<long> _notificationsFailed;
    private readonly Counter<long> _notificationRetryCount;

    private readonly Histogram<double> _notificationSendDuration;

    public NotificationMetrics()
    {
        _notificationsCreated = Meter.CreateCounter<long>("notifications_created_total");
        _notificationsSent = Meter.CreateCounter<long>("notifications_sent_total");
        _notificationsFailed = Meter.CreateCounter<long>("notifications_failed_total");
        _notificationRetryCount = Meter.CreateCounter<long>("notification_retry_count");
                // NEW HISTOGRAM
        _notificationSendDuration =
        Meter.CreateHistogram<double>(
            "notification_send_duration_seconds",
            unit: "seconds",
            description: "Time taken to send a notification");
    }

    public void NotificationCreated(string channel)
        => _notificationsCreated.Add(1, new KeyValuePair<string, object?>("channel", channel));

    public void NotificationSent(string channel)
        => _notificationsSent.Add(1, new KeyValuePair<string, object?>("channel", channel));

    public void NotificationFailed(string channel)
        => _notificationsFailed.Add(1, new KeyValuePair<string, object?>("channel", channel));

    public void NotificationRetried(string channel)
        => _notificationRetryCount.Add(1, new KeyValuePair<string, object?>("channel", channel));

    public void NotificationSendDuration(string channel,double durationSeconds)
        => _notificationSendDuration.Record(durationSeconds,
                                            new KeyValuePair<string, object?>("channel", channel));
    
}