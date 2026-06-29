using ClaimsService.Application.Models;
using MediatR;

namespace ClaimsService.Application.Queries;
public sealed record GetMyClaimsQuery
    : IRequest<IReadOnlyList<ClaimSummaryDto>>;