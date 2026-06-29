using ClaimsService.Application.Commands;
using ClaimsService.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using ClaimsService.Domain.Entities;
using ClaimsService.Domain.Enums;

namespace ClaimsService.Application.Handlers;

public class ApproveClaimHandler : IRequestHandler<ApproveClaimCommand, Guid>
{
    private readonly IClaimRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsMetrics _metrics;
    private readonly ILogger<ApproveClaimHandler> _logger;

    public ApproveClaimHandler(
        IClaimRepository repo,
        IUnitOfWork unitOfWork,
        IClaimsMetrics metrics,
        ILogger<ApproveClaimHandler> logger)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<Guid> Handle(ApproveClaimCommand command, CancellationToken cancellationToken)
    {
        var claim = await _repo.GetByIdAsync(command.ClaimId);

        if (claim == null)
            throw new Exception($"Claim with ID {command.ClaimId} not found.");

        claim.Approve();

        await _repo.AddStatusHistoryAsync(
            new ClaimStatusHistory
            {
                Id = Guid.NewGuid(),
                ClaimId = claim.Id,
                Status = ClaimStatus.Approved,
                OccurredAt = DateTime.UtcNow
            });

        await _unitOfWork.CommitAsync(cancellationToken);

        _metrics.ClaimApproved();

        _logger.LogInformation(
            "Claim approved. ClaimId={ClaimId}",
            claim.Id);

        return claim.Id;
    }
}