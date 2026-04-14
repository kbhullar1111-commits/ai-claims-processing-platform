using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DocumentService.Infrastructure.Messaging;

public class OutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitPublisher _publisher;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(
        IServiceScopeFactory scopeFactory,
        RabbitPublisher publisher,
        ILogger<OutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<DocumentDbContext>();

            var processedMessages = await db.OutboxMessages
                .Where(x => x.Processed)
                .OrderBy(x => x.CreatedAt)
                .Take(50)
                .ToListAsync(stoppingToken);

            if (processedMessages.Count > 0)
            {
                // Processed rows are no longer needed for delivery, so remove them
                // in small batches to keep the custom outbox table from growing forever.
                db.OutboxMessages.RemoveRange(processedMessages);
                await db.SaveChangesAsync(stoppingToken);
            }

            var messages = await db.OutboxMessages
                .Where(x => !x.Processed)
                .OrderBy(x => x.CreatedAt)
                .Take(20)
                .ToListAsync(stoppingToken);

            // Rows are dispatched in creation order so events leave the service
            // in roughly the same order they were written.
            foreach (var msg in messages)
            {
                try
                {
                    // Only mark the row processed after RabbitMQ accepted the publish.
                    await _publisher.PublishAsync(msg);

                    msg.Processed = true;
                    await db.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish outbox message {OutboxMessageId}", msg.Id);
                    // Stop the current batch on first failure and retry on the next loop.
                    break;
                }
            }

            // Polling keeps the implementation simple. This can later be replaced
            // with a smarter signaling strategy if throughput requirements grow.
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}