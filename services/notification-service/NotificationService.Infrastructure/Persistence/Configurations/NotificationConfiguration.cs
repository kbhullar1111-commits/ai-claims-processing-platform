using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(x => x.NotificationId);

        builder.Property(x => x.NotificationId)
            .ValueGeneratedNever();

        builder.Property(x => x.EventId)
            .IsRequired();

        builder.Property(x => x.CustomerId)
            .IsRequired();

        builder.Property(x => x.Channel)
            .IsRequired();

        builder.Property(x => x.Template)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Parameters)
            .HasColumnType("jsonb");

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.RetryCount)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.NextRetryAt);

        builder.Property(x => x.SentAt);

        builder.HasIndex(x => new { x.Status, x.NextRetryAt });

        builder.HasIndex(x => new { x.EventId, x.Channel })
            .IsUnique();
    }
}