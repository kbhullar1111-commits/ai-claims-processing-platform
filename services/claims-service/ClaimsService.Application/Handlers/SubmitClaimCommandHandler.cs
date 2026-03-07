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

    public SubmitClaimCommandHandler(
        IClaimRepository claimRepository,
        IEventPublisher eventPublisher)
    {
        _claimRepository = claimRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<Guid> Handle(SubmitClaimCommand command, CancellationToken cancellationToken)
    {
        var claim = Claim.Submit(
            command.CustomerId,
            command.PolicyId,
            command.ClaimAmount
        );

        await _claimRepository.AddAsync(claim);

        await _claimRepository.SaveChangesAsync();

        var claimSubmittedEvent = new ClaimSubmitted(
            claim.Id,
            claim.CustomerId,
            claim.PolicyId,
            claim.ClaimAmount,
            claim.SubmittedAt
        );

        await _eventPublisher.PublishAsync(claimSubmittedEvent);

        return claim.Id;
    }
}