using BuildingBlocks.Contracts.Claims;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Commands.CreateNotification;

namespace NotificationService.Infrastructure.Messaging.Consumers;

public class ClaimSubmittedConsumer : IConsumer<ClaimSubmitted>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ClaimSubmittedConsumer> _logger;

    public ClaimSubmittedConsumer(IMediator mediator, ILogger<ClaimSubmittedConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ClaimSubmitted> context)
    {
        var eventId = context.MessageId ?? Guid.NewGuid();
        var claimId = context.Message.ClaimId;
        var customerId = context.Message.CustomerId;

        _logger.LogInformation(
            "Received ClaimSubmitted event. ClaimId={ClaimId}, CustomerId={CustomerId}, EventId={EventId}",
            claimId,
            customerId,
            eventId);

        var parameters = new Dictionary<string, string>
        {
            { "ClaimId", claimId.ToString() }
        };

        var command = new CreateNotificationCommand(
            eventId,
            customerId,
            "ClaimSubmitted",
            parameters
        );

        await _mediator.Send(command);

        _logger.LogInformation(
            "Processed ClaimSubmitted event. ClaimId={ClaimId}, CustomerId={CustomerId}, EventId={EventId}",
            claimId,
            customerId,
            eventId);
    }
}