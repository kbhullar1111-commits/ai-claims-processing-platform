namespace FraudService.Infrastructure;

public sealed class FraudCheckMessagingOptions
{
    public const string SectionName = "Messaging";

    public string FraudServiceQueue { get; init; } = "fraud-service";

    public string FraudCheckCompletedTopic { get; init; } = "fraud-check-completed";

    public int MaxConcurrentCalls { get; init; } = 8;

    public int PrefetchCount { get; init; } = 32;

    public int MaxAutoLockRenewalMinutes { get; init; } = 5;

    public int HandlerRetryMaxAttempts { get; init; } = 3;

    public int HandlerRetryBaseDelayMs { get; init; } = 200;

    public int HandlerRetryMaxDelaySeconds { get; init; } = 5;

    public int MaxDeliveryAttemptsBeforeDeadLetter { get; init; } = 10;
    
}