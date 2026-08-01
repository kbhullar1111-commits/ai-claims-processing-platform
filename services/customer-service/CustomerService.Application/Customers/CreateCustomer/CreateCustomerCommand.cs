using CustomerService.Domain.Enums;

namespace CustomerService.Application.Customers.CreateCustomer;

public sealed record CreateCustomerCommand(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string? Email,
    string? Phone,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string Country,
    CommunicationPreference PreferredCommunication);