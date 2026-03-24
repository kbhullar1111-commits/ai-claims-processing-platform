namespace NotificationService.Application.Interfaces;
public interface INotificationMetrics
{
    void NotificationCreated(string channel);
    void NotificationSent(string channel);
    void NotificationFailed(string channel);
    void NotificationRetried(string channel);
    void NotificationSendDuration(string channel,double durationSeconds);
}