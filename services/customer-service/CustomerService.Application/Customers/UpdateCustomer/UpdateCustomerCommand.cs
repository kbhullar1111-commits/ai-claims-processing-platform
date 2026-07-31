using CustomerService.Domain.Enums;

namespace CustomerService.Application.Customers.UpdateCustomer;

public sealed record UpdateCustomerCommand(
    Guid CustomerId,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string Country,
    CommunicationPreference PreferredCommunication,
    CustomerStatus Status);