using MediatR;

namespace ClaimsService.Application.Commands;
public record RejectClaimCommand(
    Guid ClaimId,
    string Reason
) : IRequest<Guid>;