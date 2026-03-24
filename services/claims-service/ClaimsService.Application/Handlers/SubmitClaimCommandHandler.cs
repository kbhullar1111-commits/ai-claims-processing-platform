using MediatR;
using BuildingBlocks.Contracts.Claims;
using ClaimsService.Application.Commands;
using ClaimsService.Application.Interfaces;
using ClaimsService.Domain.Entities;

namespace ClaimsService.Application.Handlers;

public class SubmitClaimCommandHandler : IRequestHandler<SubmitClaimCommand, Guid>
{
    private readonly IClaimRepository _claimRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsMetrics _claimsMetrics;

    public SubmitClaimCommandHandler(
        IClaimRepository claimRepository,
        IEventPublisher eventPublisher,
        IUnitOfWork unitOfWork,
        IClaimsMetrics claimsMetrics)
    {
        _claimRepository = claimRepository;
        _eventPublisher = eventPublisher;
        _unitOfWork = unitOfWork;
        _claimsMetrics = claimsMetrics;
    }

    public async Task<Guid> Handle(SubmitClaimCommand command, CancellationToken cancellationToken)
    {
        var claim = Claim.Submit(
            command.CustomerId,
            command.PolicyId,
            command.ClaimAmount
        );

        var claimSubmittedEvent = new ClaimSubmitted(
            claim.Id,
            claim.CustomerId,
            claim.PolicyId,
            claim.ClaimAmount,
            claim.SubmittedAt
        );

        await _claimRepository.AddAsync(claim);

        await _eventPublisher.PublishAsync(claimSubmittedEvent);

        await _unitOfWork.CommitAsync(cancellationToken);

        _claimsMetrics.ClaimsSubmitted();

        return claim.Id;
    }
}