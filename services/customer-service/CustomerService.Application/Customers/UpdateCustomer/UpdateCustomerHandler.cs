using CustomerService.Domain.Enums;
using CustomerService.Domain.Entities;
using CustomerService.Domain.ValueObjects;
using CustomerService.Application;


namespace CustomerService.Application.Customers.UpdateCustomer;

public sealed class UpdateCustomerHandler
{
    private readonly ICustomerRepository _repository;
    private readonly ICustomerUnitOfWork _unitOfWork;

    public UpdateCustomerHandler(
        ICustomerRepository repository,
        ICustomerUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateCustomerResponse> HandleAsync(
        UpdateCustomerCommand command,
        CancellationToken cancellationToken)
    {
        UpdateCustomerValidator.ValidateCustomerData(command);

        var customer = await  _repository.GetByIdForUpdateAsync(command.CustomerId, cancellationToken);

        if (customer is null)
        {
            throw new KeyNotFoundException(
                $"Customer '{command.CustomerId}' was not found.");
        }

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

        customer.UpdateProfile(
            command.FirstName,
            command.LastName,
            contactInformation,
            address,
            command.PreferredCommunication,
            command.Status);

        await _unitOfWork.CommitAsync(cancellationToken);

        return new UpdateCustomerResponse(command.CustomerId);
    }
}