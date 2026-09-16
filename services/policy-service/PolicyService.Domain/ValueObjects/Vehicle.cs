using PolicyService.Domain.Enums;

namespace PolicyService.Domain.Entities;
public sealed record Vehicle(
    string RegistrationNumber,
    string Model,
    VehicleType VehicleType);