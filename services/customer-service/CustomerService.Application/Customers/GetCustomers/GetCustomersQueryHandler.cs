using CustomerService.Application.Customers.Mappings;

namespace CustomerService.Application.Customers.GetCustomers;

public sealed class GetCustomersQueryHandler
{
    private readonly ICustomerRepository _repository;

    public GetCustomersQueryHandler(
        ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetCustomersResponse> HandleAsync(
        GetCustomersQuery query,
        CancellationToken cancellationToken)
    {
        var customers = await _repository.GetAllAsync(cancellationToken);

        return customers
        .Select(CustomerResponseMapper.ToGetCustomersResponse)
        .ToList();
    }
}