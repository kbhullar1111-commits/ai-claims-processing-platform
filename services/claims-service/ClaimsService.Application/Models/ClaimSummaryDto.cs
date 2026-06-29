using ClaimsService.Domain.Enums;

namespace ClaimsService.Application.Models;
public sealed record ClaimSummaryDto(
    Guid ClaimId,
    Guid PolicyId,
    decimal ClaimAmount,
    ClaimStatus Status,
    DateTime SubmittedAt);