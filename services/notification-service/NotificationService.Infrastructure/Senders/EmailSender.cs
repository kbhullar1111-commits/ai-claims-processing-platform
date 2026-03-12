using Microsoft.Extensions.Logging;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Infrastructure.Senders;

public class EmailSender : INotificationSender
{
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(ILogger<EmailSender> logger)
    {
        _logger = logger;
    }

    public NotificationChannel Channel => NotificationChannel.Email;

    public Task SendAsync(Notification notification, CancellationToken cancellationToken)
    {
        notification.Parameters.TryGetValue("ClaimId", out var claimId);

        _logger.LogInformation(
            "Sending EMAIL notification. NotificationId={NotificationId}, EventId={EventId}, ClaimId={ClaimId}, CustomerId={CustomerId}",
            notification.NotificationId,
            notification.EventId,
            claimId,
            notification.CustomerId);

        return Task.CompletedTask;
    }
}