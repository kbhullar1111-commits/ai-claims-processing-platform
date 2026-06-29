namespace NotificationService.Infrastructure.Messaging;

public sealed class NotificationMessagingOptions
{
    public const string SectionName = "Messaging";

    public string NotificationServiceQueue { get; init; } = "notification-service";

    public string ClaimSubmittedTopic { get; init; } = "claim-submitted";

    public string ClaimSubmittedSubscription { get; init; } = "notification-service";

    public int MaxConcurrentCalls { get; init; } = 8;

    public int PrefetchCount { get; init; } = 32;

    public int MaxAutoLockRenewalMinutes { get; init; } = 5;

    public int HandlerRetryMaxAttempts { get; init; } = 3;

    public int HandlerRetryBaseDelayMs { get; init; } = 200;

    public int HandlerRetryMaxDelaySeconds { get; init; } = 5;

    public int MaxDeliveryAttemptsBeforeDeadLetter { get; init; } = 10;
}
