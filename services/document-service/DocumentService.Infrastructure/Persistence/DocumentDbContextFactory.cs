using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using DocumentService.Infrastructure.Persistence;

public class DocumentDbContextFactory : IDesignTimeDbContextFactory<DocumentDbContext>
{
    public DocumentDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? throw new InvalidOperationException(
                "Set the 'ConnectionStrings__Postgres' environment variable before running EF migrations.");

        var optionsBuilder = new DbContextOptionsBuilder<DocumentDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new DocumentDbContext(optionsBuilder.Options);
    }
}
