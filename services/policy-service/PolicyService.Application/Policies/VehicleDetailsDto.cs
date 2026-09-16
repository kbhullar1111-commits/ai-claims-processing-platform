using PolicyService.Domain.Enums;

namespace PolicyService.Application.Policies;

public sealed record VehicleDetailsDto(
    string RegistrationNumber,
    string Model,
    VehicleType VehicleType);