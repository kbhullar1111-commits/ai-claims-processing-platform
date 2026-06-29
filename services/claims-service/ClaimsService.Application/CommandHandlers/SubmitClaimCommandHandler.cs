using MediatR;
using BuildingBlocks.Contracts.Claims;
using ClaimsService.Application.Commands;
using ClaimsService.Application.Interfaces;
using ClaimsService.Domain.Entities;
using ClaimsService.Domain.Enums;

namespace ClaimsService.Application.Handlers;

public class SubmitClaimCommandHandler : IRequestHandler<SubmitClaimCommand, Guid>
{
    private readonly IClaimRepository _claimRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsMetrics _claimsMetrics;

    private IReadOnlyList<string> RequiredDocuments { get; init; } = new List<string>(["ID_Proof", "Accident_Photos", "Police_Report"]);


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
            claim.SubmittedAt,
            RequiredDocuments
        );

        await _claimRepository.AddAsync(claim);

        await _eventPublisher.PublishAsync(claimSubmittedEvent);

        await _claimRepository.AddStatusHistoryAsync(
            new ClaimStatusHistory
            {
                Id = Guid.NewGuid(),
                ClaimId = claim.Id,
                Status = ClaimStatus.Submitted,
                OccurredAt = DateTime.UtcNow
            });

        await _unitOfWork.CommitAsync(cancellationToken);

        _claimsMetrics.ClaimsSubmitted();

        return claim.Id;
    }
}