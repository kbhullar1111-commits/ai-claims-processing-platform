using MediatR;
using ClaimsService.Application.Models;
using ClaimsService.Application.Queries;
using ClaimsService.Application.Interfaces;

namespace ClaimsService.Application.Handlers;

public sealed class GetMyClaimsQueryHandler
    : IRequestHandler<GetMyClaimsQuery, IReadOnlyList<ClaimSummaryDto>>
{
    private readonly IClaimRepository _claimRepository;
    private readonly ICurrentUser _currentUser;
    private readonly ICustomerResolver _customerResolver;

    public GetMyClaimsQueryHandler(
        IClaimRepository claimRepository,
        ICurrentUser currentUser,
        ICustomerResolver customerResolver)
    {
        _claimRepository = claimRepository;
        _currentUser = currentUser;
        _customerResolver = customerResolver;
    }

    public async Task<IReadOnlyList<ClaimSummaryDto>> Handle(
        GetMyClaimsQuery request,
        CancellationToken cancellationToken)
    {
        var customer =
            _customerResolver.Resolve(_currentUser.UserId!);

        var claims = await _claimRepository.GetByCustomerIdAsync(
            customer.CustomerId);

        return claims
            .Select(c => new ClaimSummaryDto(
                c.Id,
                c.PolicyId,
                c.ClaimAmount,
                c.Status,
                c.SubmittedAt))
            .ToList();
    }
}