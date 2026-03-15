namespace NotificationService.Infrastructure.Workers;

public class NotificationDispatcherOptions
{
    public int PollIntervalSeconds { get; set; } = 10;

    public int RetryDelayMinutes { get; set; } = 5;

    public int MaxRetryAttempts { get; set; } = 5;

    public int BatchSize { get; set; } = 20;
}
