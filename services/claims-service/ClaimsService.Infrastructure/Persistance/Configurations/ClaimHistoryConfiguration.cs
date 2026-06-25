using ClaimsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsService.Infrastructure.Persistence.Configurations;

public class ClaimHistoryConfiguration : IEntityTypeConfiguration<ClaimStatusHistory>
{
    public void Configure(EntityTypeBuilder<ClaimStatusHistory> builder)
    {
        builder.ToTable("claim_status_histories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ClaimId)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
            
        builder.Property(c => c.OccurredAt)
            .IsRequired();

        builder.HasIndex(c => c.ClaimId);

        builder.HasOne<Claim>()
        .WithMany()
        .HasForeignKey(c => c.ClaimId)
        .OnDelete(DeleteBehavior.Cascade);
    }
}
