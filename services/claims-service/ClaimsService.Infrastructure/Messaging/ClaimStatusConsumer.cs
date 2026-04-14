using MassTransit;
using ClaimsService.Application.Commands;
using BuildingBlocks.Contracts.Claims;
using MediatR;

public class ClaimStatusConsumer :
    IConsumer<MarkClaimUnderReview>,
    IConsumer<MarkClaimApproved>,
    IConsumer<MarkClaimRejected>
{
    private readonly IMediator _mediator;

    public ClaimStatusConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<MarkClaimApproved> context)
    {
        await _mediator.Send(new ApproveClaimCommand(context.Message.ClaimId));
    }

    public async Task Consume(ConsumeContext<MarkClaimUnderReview> context)
    {
        await _mediator.Send(new MarkClaimUnderReviewCommand(context.Message.ClaimId));
    }

    public async Task Consume(ConsumeContext<MarkClaimRejected> context)
    {
        await _mediator.Send(new RejectClaimCommand(context.Message.ClaimId, context.Message.Reason));
    }
}