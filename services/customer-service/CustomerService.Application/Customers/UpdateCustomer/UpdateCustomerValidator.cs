namespace CustomerService.Application.Customers.UpdateCustomer;

public static class UpdateCustomerValidator
{
    public static void ValidateCustomerData(UpdateCustomerCommand updateCustomerCommand)
    {
        ArgumentNullException.ThrowIfNull(updateCustomerCommand);
        ArgumentException.ThrowIfNullOrWhiteSpace(updateCustomerCommand.FirstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(updateCustomerCommand.LastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(updateCustomerCommand.AddressLine1);
        ArgumentException.ThrowIfNullOrWhiteSpace(updateCustomerCommand.PostalCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(updateCustomerCommand.City);
        ArgumentException.ThrowIfNullOrWhiteSpace(updateCustomerCommand.State);
        ArgumentException.ThrowIfNullOrWhiteSpace(updateCustomerCommand.Country);


        if(string.IsNullOrWhiteSpace(updateCustomerCommand.Phone) && string.IsNullOrWhiteSpace(updateCustomerCommand.Email))
        {
            throw new ArgumentException("Either Email or PHone has to be supplied");
        }
    }
}