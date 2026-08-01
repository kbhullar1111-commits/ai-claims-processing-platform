using CustomerService.Domain.Entities;
using CustomerService.Domain.Enums;
using CustomerService.Application.Customers.GetCustomer;
using CustomerService.Application.Customers.GetCustomers;

namespace CustomerService.Application.Customers.Mappings;

public static class CustomerResponseMapper
{
    public static GetCustomerResponse ToGetCustomerResponse(Customer customer)
    {
        return new GetCustomerResponse(
            customer.Id,
            customer.FirstName,
            customer.LastName,
            customer.DateOfBirth,
            customer.ContactInformation.Email,
            customer.ContactInformation.Phone,
            new AddressResponse(
                customer.PrimaryAddress.Line1,
                customer.PrimaryAddress.Line2,
                customer.PrimaryAddress.City,
                customer.PrimaryAddress.State,
                customer.PrimaryAddress.PostalCode,
                customer.PrimaryAddress.Country),
            customer.PreferredCommunication,
            customer.Status,
            customer.CreatedAt,
            customer.UpdatedAt);
    }

    public static GetCustomersResponse ToGetCustomersResponse(Customer customer)
    {
        return new GetCustomersResponse(
            customer.Id,
            customer.FirstName,
            customer.LastName,
            customer.ContactInformation.Email,
            customer.Status);
    }
}