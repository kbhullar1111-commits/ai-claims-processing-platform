using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NotificationService.Infrastructure.Persistence;

public class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__NotificationPostgres")
            ?? throw new InvalidOperationException(
                "Set the 'ConnectionStrings__NotificationPostgres' environment variable before running EF migrations.");

        var optionsBuilder = new DbContextOptionsBuilder<NotificationDbContext>();
        optionsBuilder.UseNpgsql(connectionString,
            b => b.MigrationsAssembly("NotificationService.Infrastructure"));

        return new NotificationDbContext(optionsBuilder.Options);
    }
}
