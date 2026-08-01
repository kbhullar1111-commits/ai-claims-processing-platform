namespace CustomerService.Application.Customers.CreateCustomer;

public static class CreateCustomerValidator
{
    public static void ValidateCustomerData(CreateCustomerCommand customerCommand)
    {
        ArgumentNullException.ThrowIfNull(customerCommand);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerCommand.FirstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerCommand.LastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerCommand.AddressLine1);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerCommand.PostalCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerCommand.City);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerCommand.State);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerCommand.Country);

        if (customerCommand.DateOfBirth == default || customerCommand.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow) )
        {
            throw new ArgumentException("Date of birth is required", nameof(customerCommand.DateOfBirth));
        }

        if(string.IsNullOrWhiteSpace(customerCommand.Phone) && string.IsNullOrWhiteSpace(customerCommand.Email))
        {
            throw new ArgumentException("Either Email or PHone has to be supplied");
        }
    }
}