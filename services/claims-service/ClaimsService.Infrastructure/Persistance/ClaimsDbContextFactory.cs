using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ClaimsService.Infrastructure.Persistence;

public class ClaimsDbContextFactory : IDesignTimeDbContextFactory<ClaimsDbContext>
{
    public ClaimsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ClaimsPostgres")
            ?? throw new InvalidOperationException(
                "Set the 'ConnectionStrings__ClaimsPostgres' environment variable before running EF migrations.");

        var optionsBuilder = new DbContextOptionsBuilder<ClaimsDbContext>();
        optionsBuilder.UseNpgsql(connectionString,
            b => b.MigrationsAssembly("ClaimsService.Infrastructure"));

        return new ClaimsDbContext(optionsBuilder.Options);
    }
}
