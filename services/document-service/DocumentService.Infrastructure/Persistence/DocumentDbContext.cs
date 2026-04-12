using Microsoft.EntityFrameworkCore;
using DocumentService.Domain.Entities;

public class DocumentDbContext : DbContext
{
    public DocumentDbContext(DbContextOptions<DocumentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.DocumentType)
                  .IsRequired();

            entity.Property(x => x.ObjectKey)
                  .IsRequired();

            entity.HasIndex(x => new { x.ClaimId, x.DocumentType });
        });
    }
}