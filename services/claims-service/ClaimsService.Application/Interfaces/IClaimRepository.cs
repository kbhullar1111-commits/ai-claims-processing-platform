using ClaimsService.Domain.Entities;

namespace ClaimsService.Application.Interfaces;

public interface IClaimRepository
{
    Task AddAsync(Claim claim);

    Task<Claim?> GetByIdAsync(Guid claimId);

    Task<IReadOnlyList<Claim>> GetByCustomerIdAsync(Guid customerId);

    Task AddStatusHistoryAsync(ClaimStatusHistory history);

    Task<IReadOnlyList<ClaimStatusHistory>> GetStatusHistoryAsync(Guid claimId);

}