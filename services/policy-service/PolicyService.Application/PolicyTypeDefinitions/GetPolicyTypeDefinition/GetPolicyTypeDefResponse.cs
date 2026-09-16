using PolicyService.Domain.Enums;
using PolicyService.Domain.Entities;

namespace PolicyService.Application.PolicyTypeDefinitions;

public sealed record GetPolicyTypeDefResponse(
    Guid Id,
    string Name,
    PolicyType Type,
    decimal MaximumClaimAmount,
    IReadOnlyCollection<CoverageDetailsDto> Coverages,
    IReadOnlyCollection<VehicleType> AllowedVehicleTypes);