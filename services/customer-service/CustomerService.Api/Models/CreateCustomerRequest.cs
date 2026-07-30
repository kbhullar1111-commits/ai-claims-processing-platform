using CustomerService.Domain.Enums;

namespace CustomerService.API.Models;

public sealed class CreateCustomerRequest
{
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public DateOnly DateOfBirth { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public string City { get; init; }
    public string State { get; init; }
    public string PostalCode { get; init; }
    public string Country { get; init; }
    public CommunicationPreference PreferredCommunication  { get; init; }
}