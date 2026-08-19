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
    private readonly ICustomerClient _customerClient;

    public GetMyClaimsQueryHandler(
        IClaimRepository claimRepository,
        ICurrentUser currentUser,
        ICustomerClient customerClient)
    {
        _claimRepository = claimRepository;
        _currentUser = currentUser;
        _customerClient = customerClient;
    }

    public async Task<IReadOnlyList<ClaimSummaryDto>> Handle(
        GetMyClaimsQuery request,
        CancellationToken cancellationToken)
    {

        var customer = await _customerClient.GetByEmailAsync(
        _currentUser.Email!,
        cancellationToken);

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