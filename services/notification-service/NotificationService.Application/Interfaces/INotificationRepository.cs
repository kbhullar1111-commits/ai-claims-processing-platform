using NotificationService.Domain.Entities;

namespace NotificationService.Application.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken);

    Task<List<Notification>> GetPendingAsync(int batchSize, CancellationToken cancellationToken);
}