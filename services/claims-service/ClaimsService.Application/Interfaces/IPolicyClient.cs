using ClaimsService.Application.Models;

namespace ClaimsService.Application.Interfaces;

public interface IPolicyClient
{
    Task<ValidateClaimResponse> ValidateClaimAsync(
        ValidateClaimRequest validateClaimRequest,
        CancellationToken cancellationToken);

}