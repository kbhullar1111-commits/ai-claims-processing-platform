using PolicyService.Domain.Entities;
using PolicyService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PolicyService.Infrastructure.Persistence.Configurations;

public sealed class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.ToTable("policies");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.PolicyNumber)
            .HasMaxLength(25)
            .IsRequired();

        builder.Property(p => p.PolicyTypeDefinitionId)
            .IsRequired();
            
        builder.HasOne<PolicyTypeDefinition>()
        .WithMany()
        .HasForeignKey(p => p.PolicyTypeDefinitionId)
        .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.CustomerId)
            .IsRequired();

        builder.Property(p => p.EffectiveDate)
            .IsRequired();

        builder.Property(p => p.ExpirationDate)
            .IsRequired();

        builder.OwnsOne(p => p.Vehicle, vehicle =>
        {
            vehicle.Property(v => v.RegistrationNumber)
                .HasMaxLength(20)
                .IsRequired();

            vehicle.Property(v => v.Model)
                .HasMaxLength(100)
                .IsRequired();

            vehicle.Property(v => v.VehicleType)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
        });
    }
}