using ClaimsService.Application.Commands;
using ClaimsService.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClaimsService.Application.Handlers;

public class RejectClaimHandler : IRequestHandler<RejectClaimCommand, Guid>
{
    private readonly IClaimRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsMetrics _metrics;
    private readonly ILogger<RejectClaimHandler> _logger;

    public RejectClaimHandler(
        IClaimRepository repo,
        IUnitOfWork unitOfWork,
        IClaimsMetrics metrics,
        ILogger<RejectClaimHandler> logger)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<Guid> Handle(RejectClaimCommand command, CancellationToken cancellationToken)
    {
        var claim = await _repo.GetByIdAsync(command.ClaimId);

        if (claim == null)
            throw new Exception($"Claim with ID {command.ClaimId} not found.");

        claim.Reject();

        _metrics.ClaimRejected(command.Reason ?? "unknown");

        _logger.LogInformation(
            "Claim rejected. ClaimId={ClaimId}, Reason={Reason}",
            claim.Id,
            command.Reason);

        await _unitOfWork.CommitAsync(cancellationToken);

        return claim.Id;
    }
}