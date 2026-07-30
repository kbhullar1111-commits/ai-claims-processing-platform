using CustomerService.Domain.ValueObjects;
using CustomerService.Domain.Enums;

namespace CustomerService.Domain.Entities;

public class Customer
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public DateOnly  DateOfBirth { get; private set; }
    public ContactInformation ContactInformation { get; private set; } = default!;
    public Address PrimaryAddress { get; private set; } = default!;
    public CommunicationPreference PreferredCommunication { get; private set; }
    public CustomerStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
                    

    private Customer() { } // for ORM


    public static Customer Create(string firstName, string lastName, DateOnly dob, ContactInformation contactInfo,
    Address address, CommunicationPreference communicationPreference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        if(contactInfo is null)
        {
            throw new ArgumentException("Contact Information is missing");
        }
        
        if(address is null)
        {
            throw new ArgumentException("Contact Information is missing");
        }

        if(dob > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ArgumentException("Date of birth is invalid");
        }

        return new Customer
        {
            Id =  Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = dob,
            ContactInformation = contactInfo,
            PrimaryAddress = address,
            Status = CustomerStatus.Active,
            PreferredCommunication = communicationPreference,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    }

    public void ChangeName(string firstName, string lastName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        FirstName = firstName;
        LastName = lastName;

        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeContactInformation(ContactInformation contactInformation)
    {
        ArgumentNullException.ThrowIfNull(contactInformation);

        ContactInformation = contactInformation;

        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePrimaryAddress(Address address)
    {
        ArgumentNullException.ThrowIfNull(address);

        PrimaryAddress = address;

        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePreferredCommunication(
    CommunicationPreference preference)
    {
        if (preference == CommunicationPreference.Email &&
            string.IsNullOrWhiteSpace(ContactInformation.Email))
        {
            throw new InvalidOperationException(
                "Customer has no email."); 
        }

        if (preference == CommunicationPreference.Phone &&
            string.IsNullOrWhiteSpace(ContactInformation.Phone))
        {
            throw new InvalidOperationException(
                "Customer has no phone.");
        }

        PreferredCommunication = preference;

        UpdatedAt = DateTime.UtcNow;
    }

    public void Suspend()
    {
        if (Status == CustomerStatus.Inactive)
        {
            throw new InvalidOperationException(
                "Inactive customer cannot be suspended.");
        }

        Status = CustomerStatus.Suspended;

        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (Status == CustomerStatus.Inactive)
            return;

        Status = CustomerStatus.Inactive;

        UpdatedAt = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        if (Status != CustomerStatus.Suspended)
        {
            throw new InvalidOperationException(
                "Only suspended customers can be reactivated.");
        }

        Status = CustomerStatus.Active;

        UpdatedAt = DateTime.UtcNow;
    }


}