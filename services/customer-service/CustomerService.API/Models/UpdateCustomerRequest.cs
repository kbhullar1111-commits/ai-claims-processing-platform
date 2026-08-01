using CustomerService.Domain.Enums;

namespace CustomerService.API.Models;

public sealed class UpdateCustomerRequest
{
    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public required string AddressLine1 { get; init; }

    public string? AddressLine2 { get; init; }

    public required string City { get; init; }

    public required string State { get; init; }

    public required string PostalCode { get; init; }

    public required string Country { get; init; }

    public CommunicationPreference PreferredCommunication { get; init; }

    public CustomerStatus Status { get; init; }
}