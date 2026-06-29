using MediatR;
using ClaimsService.Application.Models;
using ClaimsService.Application.Queries;
using ClaimsService.Application.Interfaces;

namespace ClaimsService.Application.Handlers;

public sealed class GetClaimDetailQueryHandler
    : IRequestHandler<GetClaimDetailsQuery, ClaimDetailsDto?>
{
    private readonly IClaimRepository _claimRepository;
    private readonly ICurrentUser _currentUser;
    private readonly ICustomerResolver _customerResolver;

    public GetClaimDetailQueryHandler(
        IClaimRepository claimRepository,
        ICurrentUser currentUser,
        ICustomerResolver customerResolver)
    {
        _claimRepository = claimRepository;
        _currentUser = currentUser;
        _customerResolver = customerResolver;
    }

    public async Task<ClaimDetailsDto?> Handle(
        GetClaimDetailsQuery request,
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


        return new ClaimDetailsDto(
            claim.Id,
            claim.PolicyId,
            claim.ClaimAmount,
            claim.Status,
            claim.SubmittedAt);
    }
}