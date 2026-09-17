using PolicyService.Domain.Enums;

namespace PolicyService.Application.Policies;

public sealed record ValidatePolicyCommand(
    Guid CustomerId,
    string VehicleRegistrationNumber,
    CoverageType IncidentType,
    decimal ClaimAmount,
    DateTime IncidentDate);