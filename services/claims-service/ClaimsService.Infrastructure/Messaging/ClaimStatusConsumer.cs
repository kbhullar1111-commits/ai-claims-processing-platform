using MassTransit;
using ClaimsService.Application.Commands;
using BuildingBlocks.Contracts.Claims;
using MediatR;
using Microsoft.Extensions.Logging;

public class ClaimStatusConsumer :
    IConsumer<MarkClaimUnderReview>,
    IConsumer<MarkClaimApproved>,
    IConsumer<MarkClaimRejected>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ClaimStatusConsumer> _logger;

    public ClaimStatusConsumer(IMediator mediator, ILogger<ClaimStatusConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MarkClaimApproved> context)
    {
        _logger.LogInformation(
            "Received MarkClaimApproved. ClaimId={ClaimId}",
            context.Message.ClaimId);
        await _mediator.Send(new ApproveClaimCommand(context.Message.ClaimId));
    }

    public async Task Consume(ConsumeContext<MarkClaimUnderReview> context)
    {
        _logger.LogInformation(
            "Received MarkClaimUnderReview. ClaimId={ClaimId}",
            context.Message.ClaimId);
        await _mediator.Send(new MarkClaimUnderReviewCommand(context.Message.ClaimId));
    }

    public async Task Consume(ConsumeContext<MarkClaimRejected> context)
    {
        _logger.LogInformation(
            "Received MarkClaimRejected. ClaimId={ClaimId}, Reason={Reason}",
            context.Message.ClaimId,
            context.Message.Reason);
        await _mediator.Send(new RejectClaimCommand(context.Message.ClaimId, context.Message.Reason));
    }
}