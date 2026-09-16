using PolicyService.Domain.Entities;
using PolicyService.Domain.Enums;
using PolicyService.Application.Abstractions;

namespace PolicyService.Application.PolicyTypeDefinitions;

public sealed record PolicyTypeDefinitionsResponse(
    Guid Id,
    PolicyType Type,
    string Name,
    IReadOnlyCollection<CoverageDetailsDto> Coverages,
    decimal MaximumClaimAmount,
    IReadOnlyCollection<VehicleType> AllowedVehicleTypes);