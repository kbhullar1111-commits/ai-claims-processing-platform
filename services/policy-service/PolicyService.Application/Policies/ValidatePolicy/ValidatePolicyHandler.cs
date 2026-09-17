using PolicyService.Application.Abstractions;

namespace PolicyService.Application.Policies;

public sealed class ValidatePolicyHandler
{
    private readonly IPolicyRepository _policyRepository;
    private readonly IPolicyTypeDefinitionRepository _policyTypeDefinitionRepository;

    public ValidatePolicyHandler(
        IPolicyRepository policyRepository,
        IPolicyTypeDefinitionRepository policyTypeDefinitionRepository)
    {
        _policyRepository = policyRepository;
        _policyTypeDefinitionRepository = policyTypeDefinitionRepository;
    }

    public async Task<ValidatePolicyResponse> HandleAsync(
        ValidatePolicyCommand command,
        CancellationToken cancellationToken)
    {
        if (command.CustomerId == Guid.Empty)
        {
            return Ineligible("Customer ID must be provided.");
        }

        if (string.IsNullOrWhiteSpace(command.VehicleRegistrationNumber))
        {
            return Ineligible("Vehicle registration number must be provided.");
        }

        if (command.ClaimAmount <= 0)
        {
            return Ineligible("Claim amount must be greater than zero.");
        }

        var policies = await _policyRepository.GetByCustomerIdAsync(
            command.CustomerId,
            cancellationToken);

        var matchingPolicies = policies.Where(p =>
            string.Equals(
                p.Vehicle.RegistrationNumber,
                command.VehicleRegistrationNumber.Trim(),
                StringComparison.OrdinalIgnoreCase));

        var policy = matchingPolicies.FirstOrDefault(p => p.IsActiveOn(command.IncidentDate))
            ?? matchingPolicies.FirstOrDefault();

        if (policy == null)
        {
            return Ineligible("No policy was found for the customer and vehicle.");
        }

        if (!policy.IsActiveOn(command.IncidentDate))
        {
            return Ineligible(
                "The policy was not active on the incident date.",
                policy.Id,
                policy.PolicyNumber);
        }

        var policyTypeDefinition = await _policyTypeDefinitionRepository.GetByIdAsync(
            policy.PolicyTypeDefinitionId,
            cancellationToken);

        if (policyTypeDefinition == null)
        {
            return Ineligible(
                "The policy type definition could not be found.",
                policy.Id,
                policy.PolicyNumber);
        }

        var coverage = policyTypeDefinition.Coverages.FirstOrDefault(c => c.Type == command.IncidentType);
        if (coverage == null)
        {
            return Ineligible(
                "The incident type is not covered by the policy.",
                policy.Id,
                policy.PolicyNumber);
        }

        if (command.ClaimAmount > coverage.CoverageLimit)
        {
            return Ineligible(
                $"The claim amount exceeds the coverage limit of {coverage.CoverageLimit:0.00}.",
                policy.Id,
                policy.PolicyNumber);
        }

        if (command.ClaimAmount > policyTypeDefinition.MaximumClaimAmount)
        {
            return Ineligible(
                $"The claim amount exceeds the policy maximum of {policyTypeDefinition.MaximumClaimAmount:0.00}.",
                policy.Id,
                policy.PolicyNumber);
        }

        return new ValidatePolicyResponse(
            true,
            policy.Id,
            policy.PolicyNumber,
            null);
    }

    private static ValidatePolicyResponse Ineligible(
        string reason,
        Guid? policyId = null,
        string? policyNumber = null)
    {
        return new ValidatePolicyResponse(false, policyId, policyNumber, reason);
    }
}