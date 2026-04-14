using ClaimsService.Application.Commands;
using ClaimsService.Application.Interfaces;
using MediatR;

namespace ClaimsService.Application.Handlers;

public class MarkClaimUnderReviewHandler : IRequestHandler<MarkClaimUnderReviewCommand, Guid>
{
    private readonly IClaimRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public MarkClaimUnderReviewHandler(IClaimRepository repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(MarkClaimUnderReviewCommand command, CancellationToken cancellationToken)
    {
        var claim = await _repo.GetByIdAsync(command.ClaimId);

        if (claim == null)
            throw new Exception($"Claim with ID {command.ClaimId} not found.");

        claim.MarkUnderReview();

        await _unitOfWork.CommitAsync(cancellationToken);

        return claim.Id;
    }
}