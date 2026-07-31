
namespace CustomerService.Domain.ValueObjects;

public class Address
{
    public string Line1 { get; private set; }  = default!;
    public string? Line2 { get; private set; }
    public string City { get; private set; } = default!;
    public string State { get; private set; } = default!;
    public string PostalCode { get; private set; } = default!;
    public string Country { get; private set; } = default!;

    private Address(){}

    public static Address Create(string line1, string? line2, string city, string state, string postalCode, string country)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(line1);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(country);
        
        return new Address
        {
            Line1 = line1,
            Line2 = line2,
            City = city,
            State = state,
            PostalCode = postalCode,
            Country = country
        };
    }

}