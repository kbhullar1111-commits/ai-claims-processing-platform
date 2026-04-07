namespace ClaimsService.Application.Commands;
public record RunFraudCheck(
    Guid ClaimId
);