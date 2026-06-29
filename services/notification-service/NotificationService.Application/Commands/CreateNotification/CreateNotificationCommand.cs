using MediatR;

namespace NotificationService.Application.Commands.CreateNotification;

public record CreateNotificationCommand(
    Guid EventId,
    Guid CustomerId,
    string Template,
    Dictionary<string, string> Parameters
) : IRequest;