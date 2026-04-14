using MediatR;

namespace ClaimsService.Application.Commands;
public record MarkClaimUnderReviewCommand(
    Guid ClaimId
) : IRequest<Guid>;