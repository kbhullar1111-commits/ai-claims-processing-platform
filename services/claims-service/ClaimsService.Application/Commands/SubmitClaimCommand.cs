namespace ClaimsService.Application.Commands;

public record SubmitClaimCommand(
    Guid CustomerId,
    Guid PolicyId,
    decimal ClaimAmount
);