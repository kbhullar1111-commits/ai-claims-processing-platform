using ClaimsService.Application.Models;
using MediatR;

namespace ClaimsService.Application.Queries;
public sealed record GetClaimHistoryQuery(
    Guid ClaimId)
    : IRequest<IReadOnlyList<ClaimHistoryDto>?>;