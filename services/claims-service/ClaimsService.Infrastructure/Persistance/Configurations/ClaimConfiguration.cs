using ClaimsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsService.Infrastructure.Persistence.Configurations;

public class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("claims");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ClaimAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasMaxLength(50)
            .IsRequired();
    }
}
