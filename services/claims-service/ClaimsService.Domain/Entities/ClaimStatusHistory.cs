using ClaimsService.Domain.Enums;

namespace ClaimsService.Domain.Entities;

public class ClaimStatusHistory
{
    public Guid Id { get; set; }

    public Guid ClaimId { get; set; }

    public ClaimStatus Status { get; set; }

    public DateTime OccurredAt { get; set; }
}