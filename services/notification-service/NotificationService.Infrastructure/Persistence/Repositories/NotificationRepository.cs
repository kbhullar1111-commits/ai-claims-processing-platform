using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Infrastructure.Persistence.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _dbContext;
    private const string PendingNotificationsQuery = """
    SELECT *
    FROM notifications
    WHERE "Status" = {0}
    AND ("NextRetryAt" IS NULL OR "NextRetryAt" <= now())
    ORDER BY "CreatedAt"
    FOR UPDATE SKIP LOCKED
    LIMIT {1}
    """;

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

    public async Task<List<Notification>> GetPendingAsync(int batchSize, CancellationToken cancellationToken)
    {
        var effectiveBatchSize = batchSize <= 0 ? 20 : batchSize;

        var pending = (int)NotificationStatus.Pending;

        return await _dbContext.Notifications
        .FromSqlInterpolated($@"
            {PendingNotificationsQuery}",
            pending,
            effectiveBatchSize)
        .ToListAsync(cancellationToken);
    }
}
