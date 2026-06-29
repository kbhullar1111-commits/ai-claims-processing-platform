using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Infrastructure.Senders;

public interface INotificationSender
{
    NotificationChannel Channel { get; }

    Task SendAsync(Notification notification, CancellationToken cancellationToken);
}