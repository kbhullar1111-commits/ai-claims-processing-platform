namespace PolicyService.Application.Policies;
public sealed record CreatePolicyCommand(
    Guid CustomerId,
    string PolicyName,
    DateTime PolicyTypeEffectiveFrom,
    DateTime EffectiveDate,
    DateTime ExpirationDate,
    VehicleDetailsDto Vehicle);