using MediatR;
using ClaimsService.Application.Models;
using ClaimsService.Application.Queries;
using ClaimsService.Application.Interfaces;

namespace ClaimsService.Application.Handlers;

public sealed class GetClaimHistoryQueryHandler
    : IRequestHandler<GetClaimHistoryQuery, IReadOnlyList<ClaimHistoryDto>?>
{
    private readonly IClaimRepository _claimRepository;
    private readonly ICurrentUser _currentUser;
    private readonly ICustomerResolver _customerResolver;

    public GetClaimHistoryQueryHandler(
        IClaimRepository claimRepository,
        ICurrentUser currentUser,
        ICustomerResolver customerResolver)
    {
        _claimRepository = claimRepository;
        _currentUser = currentUser;
        _customerResolver = customerResolver;
    }

    public async Task<IReadOnlyList<ClaimHistoryDto>?> Handle(
        GetClaimHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var customer =
            _customerResolver.Resolve(_currentUser.UserId!);

        var claim = await _claimRepository.GetByIdAsync(
            request.ClaimId);

        if (claim == null)
        {
            return null;
        }

        if (claim.CustomerId != customer.CustomerId)
        {
            return null;
        }

        var history = await _claimRepository.GetStatusHistoryAsync(
            request.ClaimId);

        return history
            .Select(c => new ClaimHistoryDto(
                c.Status,
                c.OccurredAt))
            .ToList();
    }
}