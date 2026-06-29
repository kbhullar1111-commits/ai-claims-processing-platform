using ClaimsService.Domain.Enums;

namespace ClaimsService.Application.Models;

public sealed record ClaimDetailsDto(
    Guid ClaimId,
    Guid PolicyId,
    decimal ClaimAmount,
    ClaimStatus Status,
    DateTime SubmittedAt);