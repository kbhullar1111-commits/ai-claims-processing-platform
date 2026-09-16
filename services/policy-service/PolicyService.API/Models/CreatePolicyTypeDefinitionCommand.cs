using PolicyService.Application.PolicyTypeDefinitions;
using PolicyService.Domain.Enums;

namespace PolicyService.API.Models;

public sealed record CreatePolicyTypeDefinitionRequest(
    PolicyType Type,
    string Name,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    IReadOnlyCollection<CoverageDetailsDto> Coverages,
    decimal MaximumClaimAmount,
    IReadOnlyCollection<VehicleType> AllowedVehicleTypes);
