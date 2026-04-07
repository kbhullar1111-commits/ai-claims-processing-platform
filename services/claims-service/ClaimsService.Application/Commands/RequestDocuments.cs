namespace ClaimsService.Application.Commands;
public record RequestDocuments(
    Guid ClaimId,
    Guid CustomerId,
    IEnumerable<string> Documents
);