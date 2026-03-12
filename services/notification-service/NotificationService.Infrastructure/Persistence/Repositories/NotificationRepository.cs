using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Infrastructure.Persistence.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _dbContext;

    public NotificationRepository(NotificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken)
    {
        await _dbContext.Notifications.AddAsync(notification, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await _dbContext.Notifications
            .AnyAsync(x => x.EventId == eventId, cancellationToken);
    }

    public async Task<List<Notification>> GetPendingAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Notifications
            .Where(x => x.Status == NotificationStatus.Pending &&
                (x.NextRetryAt == null || x.NextRetryAt <= DateTime.UtcNow))
            .OrderBy(x => x.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);
    }
}
