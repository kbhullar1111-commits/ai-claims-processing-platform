
namespace CustomerService.Domain.ValueObjects;

public class ContactInformation
{
    public string? Email { get; private set; }
    public string? Phone { get; private set; }

    private ContactInformation(){}

    public static ContactInformation Create(string email, string phone)
    {
        
        if(string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
        {
            throw new ArgumentException("Either email or phone should have value");
        }

        if (!string.IsNullOrWhiteSpace(email) && !email.Contains("@"))
        {
            throw new ArgumentException("Email is invalid");
        }

        return new ContactInformation
        {
            Email = email,
            Phone = phone
        };
    }
}