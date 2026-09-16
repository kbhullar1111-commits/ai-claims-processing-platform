using PolicyService.Domain.Entities;
using PolicyService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PolicyService.Infrastructure.Persistence.Configurations;

public sealed class PolicyTypeDefConfiguration : IEntityTypeConfiguration<PolicyTypeDefinition>
{
    public void Configure(EntityTypeBuilder<PolicyTypeDefinition> builder)
    {
        builder.ToTable("policy_type_definitions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.Type)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        
        builder.Property(p => p.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.EffectiveFrom)
            .IsRequired();

        builder.Property(p => p.EffectiveTo)
            .IsRequired(false);

        builder.Property(p => p.MaximumClaimAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(p => p.AllowedVehicleTypes)
            .HasConversion(
                v => string.Join(',', v.Select(t => t.ToString())),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(t => Enum.Parse<VehicleType>(t))
                      .ToList())
            .HasMaxLength(200)
            .IsRequired();

        builder.OwnsMany(x => x.Coverages, coverage =>
        {
            coverage.ToTable("PolicyTypeDefinitionCoverages");

            coverage.Property(x => x.Type)
                .HasConversion<string>()
                .IsRequired();

            coverage.Property(x => x.CoverageLimit)
                .HasPrecision(18, 2)
                .IsRequired();
        });
    }
}