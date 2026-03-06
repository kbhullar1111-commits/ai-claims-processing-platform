using ClaimsService.Domain.Entities;

namespace ClaimsService.Application.Interfaces;

public interface IClaimRepository
{
    Task AddAsync(Claim claim);

    Task<Claim?> GetByIdAsync(Guid claimId);

    Task SaveChangesAsync();
}