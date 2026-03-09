using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Entities;

public class Notification
{
    public Guid NotificationId { get; private set; }

    public Guid EventId { get; private set; }

    public Guid CustomerId { get; private set; }

    public NotificationChannel Channel { get; private set; }

    public string Template { get; private set; }

    public Dictionary<string, string> Parameters { get; private set; }

    public NotificationStatus Status { get; private set; }

    public int RetryCount { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? SentAt { get; private set; }

    public DateTime? NextRetryAt { get; private set; }

    private Notification() // EF Core
    {
        Template = string.Empty;
        Parameters = new Dictionary<string, string>();
    }

    private Notification(
        Guid eventId,
        Guid customerId,
        NotificationChannel channel,
        string template,
        Dictionary<string, string> parameters)
    {
        if (string.IsNullOrWhiteSpace(template))
            throw new ArgumentException("Template cannot be empty");

        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        NotificationId = Guid.NewGuid();
        EventId = eventId;
        CustomerId = customerId;
        Channel = channel;
        Template = template;

        Parameters = new Dictionary<string, string>(parameters);

        Status = NotificationStatus.Pending;
        RetryCount = 0;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
    }

    public void ScheduleRetry(int retryDelayMinutes)
    {
        RetryCount++;
        NextRetryAt = DateTime.UtcNow.AddMinutes(retryDelayMinutes);
    }

    public void MarkFailed()
    {
        Status = NotificationStatus.Failed;
    }

    public static Notification Create(
        Guid eventId,
        Guid customerId,
        NotificationChannel channel,
        string template,
        Dictionary<string, string> parameters)
    {
        return new Notification(eventId, customerId, channel, template, parameters);
    }
}