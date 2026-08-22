using CustomerService.Domain.Entities;
using CustomerService.Application.Customers.Mappings;

namespace CustomerService.Application.Customers.GetCustomer;

public sealed class GetCustomerQueryHandler
{
    private readonly ICustomerRepository _repository;

    public GetCustomerQueryHandler(
        ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetCustomerResponse> HandleAsync(
        GetCustomerQuery query,
        CancellationToken cancellationToken)
    {
        Customer? customer;

        if (query.Id.HasValue)
        {
            customer = await _repository.GetByIdAsync(
                query.Id.Value,
                cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(query.Email))
        {
            customer = await _repository.GetByEmailAsync(
                query.Email,
                cancellationToken);
        }
        else
        {
            throw new ArgumentException(
                "Either Customer Id or Email must be provided.");
        }

        if (customer is null)
        {
            throw new KeyNotFoundException("Customer was not found.");
        }

        return CustomerResponseMapper.ToGetCustomerResponse(customer);
    }
}