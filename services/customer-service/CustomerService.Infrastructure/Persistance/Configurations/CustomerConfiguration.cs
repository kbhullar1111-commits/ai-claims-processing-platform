using CustomerService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerService.Infrastructure.Persistence;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.DateOfBirth)
            .IsRequired();

        builder.Property(c => c.PreferredCommunication)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        builder.OwnsOne(c => c.ContactInformation, contact =>
        {
            contact.Property(c => c.Email)
                .HasMaxLength(256)
                .IsRequired();

            contact.Property(c => c.Phone)
                .HasMaxLength(20)
                .IsRequired();
        });

        builder.OwnsOne(c => c.PrimaryAddress, address =>
        {
            address.Property(a => a.Line1)
                .HasMaxLength(200)
                .IsRequired();

            address.Property(a => a.Line2)
                .HasMaxLength(200);

            address.Property(a => a.City)
                .HasMaxLength(100)
                .IsRequired();

            address.Property(a => a.State)
                .HasMaxLength(100)
                .IsRequired();

            address.Property(a => a.PostalCode)
                .HasMaxLength(20)
                .IsRequired();

            address.Property(a => a.Country)
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.HasIndex(c => c.Status);

        builder.HasIndex(c => c.LastName);

    }
}