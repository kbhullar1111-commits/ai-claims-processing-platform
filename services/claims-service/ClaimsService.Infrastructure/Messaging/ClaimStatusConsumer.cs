using MassTransit;
using ClaimsService.Application.Commands;
using ClaimsService.Application.Interfaces;
using BuildingBlocks.Contracts.Claims;
using ClaimsService.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

public class ClaimStatusConsumer :
    IConsumer<MarkClaimUnderReview>,
    IConsumer<MarkClaimApproved>,
    IConsumer<MarkClaimRejected>
{
    private readonly IMediator _mediator;
    private readonly IClaimRepository _repo;
    private readonly ILogger<ClaimStatusConsumer> _logger;

    public ClaimStatusConsumer(IMediator mediator, IClaimRepository repo, ILogger<ClaimStatusConsumer> logger)
    {
        _mediator = mediator;
        _repo = repo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MarkClaimApproved> context)
    {
        _logger.LogInformation(
            "Received MarkClaimApproved. ClaimId={ClaimId}",
            context.Message.ClaimId);

        var claim = await _repo.GetByIdAsync(context.Message.ClaimId);
        if (claim is null)
        {
            _logger.LogWarning(
                "MarkClaimApproved ignored — claim not found. ClaimId={ClaimId}",
                context.Message.ClaimId);
            return;
        }

        if (claim.Status == ClaimStatus.Approved)
        {
            _logger.LogWarning(
                "Duplicate MarkClaimApproved ignored — claim already approved. ClaimId={ClaimId}",
                context.Message.ClaimId);
            return;
        }

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

        var claim = await _repo.GetByIdAsync(context.Message.ClaimId);
        if (claim is null)
        {
            _logger.LogWarning(
                "MarkClaimRejected ignored — claim not found. ClaimId={ClaimId}",
                context.Message.ClaimId);
            return;
        }

        if (claim.Status == ClaimStatus.Rejected)
        {
            _logger.LogWarning(
                "Duplicate MarkClaimRejected ignored — claim already rejected. ClaimId={ClaimId}",
                context.Message.ClaimId);
            return;
        }

        await _mediator.Send(new RejectClaimCommand(context.Message.ClaimId, context.Message.Reason));
    }
}