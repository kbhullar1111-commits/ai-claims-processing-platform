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
        var customer = await _repository.GetByIdAsync(query.Id, cancellationToken);

        if (customer is null)
        {
            throw new KeyNotFoundException(
                $"Customer '{query.Id}' was not found.");
        }

        return CustomerResponseMapper.ToGetCustomerResponse(customer);
    }
}