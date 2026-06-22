public sealed class SubmitClaimRequest
{
    public string PolicyId { get; init; } = default!;
    public decimal ClaimAmount { get; init; }
}