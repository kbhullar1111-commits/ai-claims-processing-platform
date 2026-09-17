public sealed class SubmitClaimRequest
{
    public string VehicleRegistrationNumber { get; init; } = string.Empty;
    public string IncidentType { get; init; } = string.Empty;
    public decimal ClaimAmount { get; init; }
    public DateTime IncidentDate { get; init; }
}
