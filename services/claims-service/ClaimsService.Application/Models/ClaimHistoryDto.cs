using ClaimsService.Domain.Enums;

namespace ClaimsService.Application.Models;

public sealed record ClaimHistoryDto(
    ClaimStatus Status,
    DateTime OccurredAt);