namespace ClaimsService.Application.Interfaces;

public interface IClaimsMetrics
{
    void ClaimsSubmitted();
    void ClaimApproved();
    void ClaimRejected(string reason);
}