using ClaimsService.Domain.Enums;

namespace ClaimsService.Domain.Entities;

public class Claim
{
    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid PolicyId { get; private set; }

    public decimal ClaimAmount { get; private set; }

    public ClaimStatus Status { get; private set; }

    public DateTime SubmittedAt { get; private set; }

    private Claim() { } // for ORM

    public Claim(Guid customerId, Guid policyId, decimal claimAmount)
    {
        if (claimAmount <= 0)
            throw new ArgumentException("Claim amount must be greater than zero");

        Id = Guid.NewGuid();
        CustomerId = customerId;
        PolicyId = policyId;
        ClaimAmount = claimAmount;
        Status = ClaimStatus.Submitted;
        SubmittedAt = DateTime.UtcNow;
    }

    public void Approve()
    {
        if (Status != ClaimStatus.UnderReview)
            throw new InvalidOperationException("Claim cannot be approved in current state");

        Status = ClaimStatus.Approved;
    }

    public void Reject()
    {
        if (Status != ClaimStatus.UnderReview)
            throw new InvalidOperationException("Claim cannot be rejected in current state");

        Status = ClaimStatus.Rejected;
    }

    public void MarkUnderReview()
    {
        if (Status != ClaimStatus.Submitted)
            throw new InvalidOperationException("Claim cannot move to review");

        Status = ClaimStatus.UnderReview;
    }

    public void MarkPaid()
    {
        if (Status != ClaimStatus.Approved)
            throw new InvalidOperationException("Only approved claims can be paid");

        Status = ClaimStatus.Paid;
    }

    public void Close()
    {
        if (Status != ClaimStatus.Paid && Status != ClaimStatus.Rejected)
            throw new InvalidOperationException("Only paid or rejected claims can be closed");

        Status = ClaimStatus.Closed;
    }
}