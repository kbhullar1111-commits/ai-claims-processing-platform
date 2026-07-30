using Microsoft.EntityFrameworkCore;
using CustomerService.Domain.Entities;

namespace CustomerService.Infrastructure.Persistence;

public sealed class CustomerDbContext : DbContext
{
    public DbSet<Customer> Customers => Set<Customer>();

    public CustomerDbContext(DbContextOptions<CustomerDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustomerDbContext).Assembly);

    }
}
