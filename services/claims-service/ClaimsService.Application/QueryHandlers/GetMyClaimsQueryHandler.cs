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

        var customerId = await _customerClient.GetCustomerIdByEmailAsync(
        _currentUser.Email!,
        cancellationToken);

        if (customerId is not Guid resolvedCustomerId)
        {
            return [];
        }

        var claims = await _claimRepository.GetByCustomerIdAsync(
            resolvedCustomerId);

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