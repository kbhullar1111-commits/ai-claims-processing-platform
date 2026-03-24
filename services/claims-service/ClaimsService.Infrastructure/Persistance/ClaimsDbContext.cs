using ClaimsService.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using ClaimsService.Application.Sagas;

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
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClaimsDbContext).Assembly);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.AddSagaStateEntity<ClaimProcessingSagaState>();

        modelBuilder.Entity<ClaimProcessingSagaState>()
            .Property(x => x.RequiredDocuments)
            .HasColumnType("text")
            .HasConversion(
                v => string.Join(",", v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());

        modelBuilder.Entity<ClaimProcessingSagaState>()
            .Property(x => x.UploadedDocuments)
            .HasColumnType("text")
            .HasConversion(
                v => string.Join(",", v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());

    }
}