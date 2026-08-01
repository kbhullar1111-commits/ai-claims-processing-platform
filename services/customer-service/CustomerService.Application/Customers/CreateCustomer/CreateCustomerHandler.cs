using CustomerService.Domain.Enums;
using CustomerService.Domain.Entities;
using CustomerService.Domain.ValueObjects;
using CustomerService.Application;


namespace CustomerService.Application.Customers.CreateCustomer;

public sealed class CreateCustomerHandler
{
    private readonly ICustomerRepository _repository;
    private readonly ICustomerUnitOfWork _unitOfWork;

    public CreateCustomerHandler(
        ICustomerRepository repository,
        ICustomerUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateCustomerResponse> HandleAsync(
        CreateCustomerCommand command,
        CancellationToken cancellationToken)
    {
        CreateCustomerValidator.ValidateCustomerData(command);

        var contactInformation = ContactInformation.Create(
            command.Email,
            command.Phone);

        var address = Address.Create(
            command.AddressLine1,
            command.AddressLine2,
            command.City,
            command.State,
            command.PostalCode,
            command.Country);

        var customer = Customer.Create(
            command.FirstName,
            command.LastName,
            command.DateOfBirth,
            contactInformation,
            address,
            command.PreferredCommunication);

        await _repository.AddAsync(customer, cancellationToken);

        await _unitOfWork.CommitAsync(cancellationToken);

        return new CreateCustomerResponse(customer.Id);
    }
}