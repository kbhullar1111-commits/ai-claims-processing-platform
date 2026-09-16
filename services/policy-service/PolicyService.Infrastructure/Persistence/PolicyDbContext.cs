using Microsoft.EntityFrameworkCore;
using PolicyService.Domain.Entities;

namespace PolicyService.Infrastructure.Persistence;

public sealed class PolicyDbContext : DbContext
{
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<PolicyTypeDefinition> PolicyTypeDefinitions => Set<PolicyTypeDefinition>();

    public PolicyDbContext(DbContextOptions<PolicyDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PolicyDbContext).Assembly);

    }
}