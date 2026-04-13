using MassTransit;
using ClaimsService.Application.Commands;
using MediatR;

public class MarkClaimApprovedConsumer : IConsumer<MarkClaimApproved>
{
    private readonly IMediator _mediator;

    public MarkClaimApprovedConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<MarkClaimApproved> context)
    {
        await _mediator.Send(new ApproveClaimCommand(context.Message.ClaimId));
    }
}