using PolicyService.Domain.Entities;
using PolicyService.Domain.Enums;

namespace PolicyService.Application.PolicyTypeDefinitions;

public sealed record CreatePolicyTypeDefinitionCommand(
    PolicyType Type,
    string Name,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    IReadOnlyCollection<CoverageDetailsDto> Coverages,
    decimal MaximumClaimAmount,
    IReadOnlyCollection<VehicleType> AllowedVehicleTypes);