using MediatR;

namespace ClaimsService.Application.Commands;
public record ApproveClaimCommand(
    Guid ClaimId
) : IRequest<Guid>;