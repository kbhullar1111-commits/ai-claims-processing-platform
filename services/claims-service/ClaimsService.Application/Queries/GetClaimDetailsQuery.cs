using ClaimsService.Application.Models;
using MediatR;

namespace ClaimsService.Application.Queries;
public sealed record GetClaimDetailsQuery(
    Guid ClaimId)
    : IRequest<ClaimDetailsDto?>;