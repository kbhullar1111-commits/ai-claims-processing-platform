using PolicyService.Domain.Enums;

namespace PolicyService.Application.Policies;
public readonly record struct GetPoliciesResponse(
    string PolicyNumber,
    PolicyStatus Status,
    DateTime EffectiveDate,
    DateTime ExpirationDate,
    string VehicleRegistrationNumber,
    string VehicleModel);