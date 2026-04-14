using Microsoft.EntityFrameworkCore;
using DocumentService.Domain.Entities;

public class DocumentDbContext : DbContext
{
    public DocumentDbContext(DbContextOptions<DocumentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.DocumentType)
                  .IsRequired();

            entity.Property(x => x.ObjectKey)
                  .IsRequired();

            entity.HasIndex(x => new { x.ClaimId, x.DocumentType });
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Type)
                  .IsRequired();

            entity.Property(x => x.Payload)
                  .IsRequired();
        });
    }
}