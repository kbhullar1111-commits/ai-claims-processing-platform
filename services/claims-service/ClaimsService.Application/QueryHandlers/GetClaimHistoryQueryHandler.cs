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
    private readonly ICustomerClient _customerClient;

    public GetClaimHistoryQueryHandler(
        IClaimRepository claimRepository,
        ICurrentUser currentUser,
        ICustomerClient customerClient)
    {
        _claimRepository = claimRepository;
        _currentUser = currentUser;
        _customerClient = customerClient;
    }

    public async Task<IReadOnlyList<ClaimHistoryDto>?> Handle(
        GetClaimHistoryQuery request,
        CancellationToken cancellationToken)
    {

        var customer = await _customerClient.GetByEmailAsync(
        _currentUser.Email!,
        cancellationToken);

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