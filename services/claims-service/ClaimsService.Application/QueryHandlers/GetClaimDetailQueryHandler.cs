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
    private readonly ICustomerClient _customerClient;

    public GetClaimDetailQueryHandler(
        IClaimRepository claimRepository,
        ICurrentUser currentUser,
        ICustomerClient customerClient)
    {
        _claimRepository = claimRepository;
        _currentUser = currentUser;
        _customerClient = customerClient;
    }

    public async Task<ClaimDetailsDto?> Handle(
        GetClaimDetailsQuery request,
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


        return new ClaimDetailsDto(
            claim.Id,
            claim.PolicyId,
            claim.ClaimAmount,
            claim.Status,
            claim.SubmittedAt);
    }
}