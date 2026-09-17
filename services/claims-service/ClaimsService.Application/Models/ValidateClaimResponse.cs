namespace ClaimsService.Application.Models;

public sealed record ValidateClaimResponse(
    bool Eligible,
    Guid? PolicyId,
    string? PolicyNumber,
    string? Reason);