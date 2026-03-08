using ClaimsService.Application.Interfaces;
using ClaimsService.Domain.Entities;
using ClaimsService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClaimsService.Infrastructure.Repositories;

public class ClaimRepository : IClaimRepository
{
    private readonly ClaimsDbContext _dbContext;

    public ClaimRepository(ClaimsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Claim claim)
    {
        await _dbContext.Claims.AddAsync(claim);
    }

    public async Task<Claim?> GetByIdAsync(Guid claimId)
    {
        return await _dbContext.Claims
            .FirstOrDefaultAsync(c => c.Id == claimId);
    }
}