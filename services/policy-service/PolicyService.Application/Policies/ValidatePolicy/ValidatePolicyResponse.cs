namespace PolicyService.Application.Policies;

public sealed record ValidatePolicyResponse(
    bool Eligible,
    Guid? PolicyId,
    string? PolicyNumber,
    string? Reason);