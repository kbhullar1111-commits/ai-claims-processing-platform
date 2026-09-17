namespace PolicyService.API.Models;

public sealed record ValidateClaimRequest(
    Guid CustomerId,
    string VehicleRegistrationNumber,
    string IncidentType,
    decimal ClaimAmount,
    DateTime IncidentDate);