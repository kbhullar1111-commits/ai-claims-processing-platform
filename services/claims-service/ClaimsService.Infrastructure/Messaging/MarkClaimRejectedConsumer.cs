using MassTransit;
using ClaimsService.Application.Commands;
using MediatR;

public class MarkClaimRejectedConsumer : IConsumer<MarkClaimRejected>
{
    private readonly IMediator _mediator;

    public MarkClaimRejectedConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<MarkClaimRejected> context)
    {
        await _mediator.Send(new RejectClaimCommand(context.Message.ClaimId, context.Message.Reason));
    }
}