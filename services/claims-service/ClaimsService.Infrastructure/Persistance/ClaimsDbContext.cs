using ClaimsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClaimsService.Infrastructure.Persistence;

public class ClaimsDbContext : DbContext
{
    public ClaimsDbContext(DbContextOptions<ClaimsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Claim> Claims => Set<Claim>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Claim>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.ClaimAmount)
                  .IsRequired();

            entity.Property(c => c.Status)
                  .IsRequired();
        });
    }
}