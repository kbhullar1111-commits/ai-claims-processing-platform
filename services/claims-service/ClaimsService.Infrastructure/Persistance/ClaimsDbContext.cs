using ClaimsService.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ClaimsService.Application.Sagas;
using System.Text.Json;

namespace ClaimsService.Infrastructure.Persistence;

public class ClaimsDbContext : DbContext
{
    public ClaimsDbContext(DbContextOptions<ClaimsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Claim> Claims => Set<Claim>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var stringListComparer = new ValueComparer<List<string>>(
            (left, right) => left == null && right == null ||
                             left != null && right != null && left.SequenceEqual(right),
            list => list == null
                ? 0
                : list.Aggregate(0, (current, item) => HashCode.Combine(current, item == null ? 0 : item.GetHashCode())),
            list => list == null ? new List<string>() : list.ToList());

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClaimsDbContext).Assembly);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<ClaimProcessingSagaState>()
            .HasKey(x => x.CorrelationId);

        modelBuilder.Entity<ClaimProcessingSagaState>()
            .Property(x => x.RequiredDocuments)
            .HasColumnType("text")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => ParseStoredList(v))
            .Metadata.SetValueComparer(stringListComparer);

        modelBuilder.Entity<ClaimProcessingSagaState>()
            .Property(x => x.UploadedDocuments)
            .HasColumnType("text")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => ParseStoredList(v))
            .Metadata.SetValueComparer(stringListComparer);

    }

    private static List<string> ParseStoredList(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
            return new List<string>();

        var trimmed = stored.Trim();

        if (trimmed.StartsWith("["))
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(trimmed) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        return trimmed
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }
}