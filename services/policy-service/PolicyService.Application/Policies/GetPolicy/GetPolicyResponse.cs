using PolicyService.Domain.Entities;
using PolicyService.Domain.Enums;

namespace PolicyService.Application.Policies;

public sealed record GetPolicyResponse(
    Guid Id,
    string PolicyNumber,
    Guid PolicyTypeDefinitionId,
    PolicyStatus Status,
    Guid CustomerId,
    DateTime EffectiveDate,
    DateTime ExpirationDate,
    VehicleDetailsDto VehicleDetails);