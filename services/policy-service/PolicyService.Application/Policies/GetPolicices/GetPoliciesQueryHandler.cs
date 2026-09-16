using PolicyService.Domain.Entities;
using PolicyService.Domain.Enums;
using PolicyService.Application.Abstractions;

namespace PolicyService.Application.Policies;

public sealed class GetPoliciesQueryHandler
{
    private readonly IPolicyRepository _policyRepository;

    public GetPoliciesQueryHandler(IPolicyRepository policyRepository)
    {
        _policyRepository = policyRepository;
    }

    public async Task<List<GetPoliciesResponse>> HandleAsync(GetPoliciesQuery query, CancellationToken cancellationToken)
    {
        var policies = await _policyRepository.GetByCustomerIdAsync(query.CustomerId, cancellationToken);

        var myPolicies = policies.Select(policy => new GetPoliciesResponse(
            policy.PolicyNumber,
            policy.Status,
            policy.EffectiveDate,
            policy.ExpirationDate,
            policy.Vehicle!.RegistrationNumber,
            policy.Vehicle!.Model))
            .ToList();

        return myPolicies;
    }
}


