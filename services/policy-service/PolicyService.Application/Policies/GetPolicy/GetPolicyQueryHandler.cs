using PolicyService.Domain.Entities;
using PolicyService.Domain.Enums;
using PolicyService.Application.Abstractions;

namespace PolicyService.Application.Policies;

public sealed class GetPolicyQueryHandler
{
    private readonly IPolicyRepository _policyRepository;

    public GetPolicyQueryHandler(IPolicyRepository policyRepository)
    {
        _policyRepository = policyRepository;
    }

    public async Task<GetPolicyResponse> HandleAsync(GetPolicyQuery query, CancellationToken cancellationToken)
    {

        var policy = await _policyRepository.GetByIdAsync(query.Id, cancellationToken);

        if (policy == null)
        {
            throw new KeyNotFoundException($"Policy with ID {query.Id} not found.");
        }


        var vehicleDetails = new VehicleDetailsDto(
            policy.Vehicle!.RegistrationNumber,
            policy.Vehicle!.Model,
            policy.Vehicle!.VehicleType);

        return new GetPolicyResponse(
            policy.Id,
            policy.PolicyNumber,
            policy.PolicyTypeDefinitionId,
            policy.Status,
            policy.CustomerId,
            policy.EffectiveDate,
            policy.ExpirationDate,
            vehicleDetails);
    }
}