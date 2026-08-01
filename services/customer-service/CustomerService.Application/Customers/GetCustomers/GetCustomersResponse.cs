using CustomerService.Domain.Enums;

namespace CustomerService.Application.Customers.GetCustomers;

public sealed record GetCustomersResponse(
    Guid CustomerId,
    string FirstName,
    string LastName,
    string? Email,
    CustomerStatus Status);