using PolicyService.Domain.Enums;

namespace PolicyService.API.Models;

public sealed record CreatePolicyRequest(
    Guid CustomerId,
    string PolicyName,
    DateTime PolicyTypeEffectiveFrom,
    DateTime EffectiveDate,
    DateTime ExpirationDate,
    string RegistrationNumber,
    string Model,
    VehicleType VehicleType
);