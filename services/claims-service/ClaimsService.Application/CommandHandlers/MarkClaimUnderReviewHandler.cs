using ClaimsService.Application.Commands;
using ClaimsService.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClaimsService.Application.Handlers;

public class MarkClaimUnderReviewHandler : IRequestHandler<MarkClaimUnderReviewCommand, Guid>
{
    private readonly IClaimRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MarkClaimUnderReviewHandler> _logger;

    public MarkClaimUnderReviewHandler(
        IClaimRepository repo,
        IUnitOfWork unitOfWork,
        ILogger<MarkClaimUnderReviewHandler> logger)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> Handle(MarkClaimUnderReviewCommand command, CancellationToken cancellationToken)
    {
        var claim = await _repo.GetByIdAsync(command.ClaimId);

        if (claim == null)
            throw new Exception($"Claim with ID {command.ClaimId} not found.");

        claim.MarkUnderReview();

        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Claim marked under review. ClaimId={ClaimId}",
            claim.Id);

        return claim.Id;
    }
}