using CustomerService.Domain.Enums;

namespace CustomerService.Application.Customers.GetCustomer;

public sealed record GetCustomerResponse(
    Guid CustomerId,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,

    string? Email,
    string? Phone,

    AddressResponse Address,

    CommunicationPreference PreferredCommunication,

    CustomerStatus Status,

    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record AddressResponse(
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string Country);