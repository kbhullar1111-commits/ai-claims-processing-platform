using MediatR;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Application.Commands.CreateNotification;

public class CreateNotificationCommandHandler 
    : IRequestHandler<CreateNotificationCommand>
{
    private readonly INotificationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateNotificationCommandHandler> _logger;
    private readonly INotificationMetrics _metrics;

    public CreateNotificationCommandHandler(
        INotificationRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateNotificationCommandHandler> logger,
        INotificationMetrics metrics)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        var exists = await _repository.ExistsAsync(request.EventId, cancellationToken);
        request.Parameters.TryGetValue("ClaimId", out var claimId);

        if (exists)
        {
            _logger.LogInformation(
                "Skipping duplicate notification creation. EventId={EventId}, ClaimId={ClaimId}, CustomerId={CustomerId}",
                request.EventId,
                claimId,
                request.CustomerId);
            return;
        }

        var notification = Notification.Create(
            request.EventId,
            request.CustomerId,
            NotificationChannel.Email,
            request.Template,
            request.Parameters
        );

        await _repository.AddAsync(notification, cancellationToken);

        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Notification persisted. NotificationId={NotificationId}, EventId={EventId}, ClaimId={ClaimId}, CustomerId={CustomerId}",
            notification.NotificationId,
            request.EventId,
            claimId,
            request.CustomerId);
            
        _metrics.NotificationCreated(NotificationChannel.Email.ToString());
    }
}