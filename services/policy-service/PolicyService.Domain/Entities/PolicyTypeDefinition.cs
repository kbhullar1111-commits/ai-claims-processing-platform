using PolicyService.Domain.Enums;

namespace PolicyService.Domain.Entities;

public sealed class PolicyTypeDefinition
{
    public Guid Id { get; init; }
    public PolicyType Type { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public ICollection<Coverage> Coverages { get; init; }
        = new List<Coverage>();

    public decimal MaximumClaimAmount { get; init; }

    public IReadOnlyCollection<VehicleType> AllowedVehicleTypes { get; init; }
        = Array.Empty<VehicleType>();

    private PolicyTypeDefinition()
    {
        // Private constructor to enforce the use of the factory method.
    }

    public static PolicyTypeDefinition Create(
        PolicyType type,
        string name,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        IEnumerable<Coverage> coverages,
        decimal maximumClaimAmount,
        IEnumerable<VehicleType> allowedVehicleTypes)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Policy type definition name cannot be empty.");
        }
        if (maximumClaimAmount <= 0)
        {
            throw new ArgumentException("Maximum claim amount must be greater than zero.");
        }
        if (effectiveTo.HasValue && effectiveTo <= effectiveFrom)
        {
            throw new ArgumentException("Effective to date must be greater than effective from date.");
        }

        return new PolicyTypeDefinition
        {
            Id = Guid.NewGuid(),
            Type = type,
            Name = name,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            Coverages = coverages.ToList().AsReadOnly(),
            MaximumClaimAmount = maximumClaimAmount,
            AllowedVehicleTypes = allowedVehicleTypes.ToList().AsReadOnly()
        };
    }
}