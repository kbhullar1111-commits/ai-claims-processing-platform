using PolicyService.Domain.Enums;

namespace PolicyService.Domain.Entities;
public class Policy
{
    public Guid Id { get; private set; }
    public string PolicyNumber { get; private set; } = string.Empty;
    public Guid PolicyTypeDefinitionId { get; private set; }
    public PolicyStatus Status { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateTime EffectiveDate { get; private set; }
    public DateTime ExpirationDate { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;

    private Policy()
    {
        // Private constructor to enforce the use of the factory method.
        // Initialized to satisfy nullable analysis before the factory method sets the values.
        Vehicle = null!;
    }

    public void Cancel()
    {
        if (Status != PolicyStatus.Active)
        {
            throw new InvalidOperationException("Policy is not active.");
        }
        Status = PolicyStatus.Cancelled;
    }

    public bool IsActiveOn(DateTime date)
    {
        return Status == PolicyStatus.Active && date >= EffectiveDate && date <= ExpirationDate;
    }

    private static string GeneratePolicyNumber()
    {
        return $"PLN-{Guid.NewGuid():N}"[..12].ToUpperInvariant();
    }


    public static Policy Create(
        Guid policyTypeDefinitionId,
        Guid customerId,
        DateTime effectiveDate,
        DateTime expirationDate,
        Vehicle vehicleDetails)
    {
        if (effectiveDate >= expirationDate)
        {
            throw new ArgumentException("Effective date must be earlier than expiration date.");
        }
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("Customer ID cannot be empty.");
        }
        if (policyTypeDefinitionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Policy type definition ID cannot be empty.");
        }
        if (vehicleDetails == null)
        {
            throw new ArgumentNullException(nameof(vehicleDetails), "Vehicle details cannot be null.");
        }
        var policy = new Policy();
        policy.Id = Guid.NewGuid();
        policy.PolicyNumber = GeneratePolicyNumber();
        policy.PolicyTypeDefinitionId = policyTypeDefinitionId;
        policy.Status = PolicyStatus.Active; // New policies are active by default
        policy.CustomerId = customerId;
        policy.EffectiveDate = effectiveDate;
        policy.ExpirationDate = expirationDate;
        policy.Vehicle = vehicleDetails;
        return policy;
    }
}