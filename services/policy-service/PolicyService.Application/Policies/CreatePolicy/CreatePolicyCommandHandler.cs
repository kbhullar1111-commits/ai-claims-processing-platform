using PolicyService.Domain.Entities;
using PolicyService.Domain.Enums;
using PolicyService.Application.Abstractions;

namespace PolicyService.Application.Policies;

public sealed class CreatePolicyCommandHandler
{
    private readonly IPolicyRepository _policyRepository;
    private readonly IPolicyTypeDefinitionRepository _policyTypeDefinitionRepository;

    private readonly IPolicyUnitOfWork _policyUnitOfWork;

    public CreatePolicyCommandHandler(
        IPolicyRepository policyRepository,
        IPolicyTypeDefinitionRepository policyTypeDefinitionRepository,
        IPolicyUnitOfWork policyUnitOfWork)
    {
        _policyRepository = policyRepository;
        _policyTypeDefinitionRepository = policyTypeDefinitionRepository;
        _policyUnitOfWork = policyUnitOfWork;
    }

    public async Task<CreatePolicyResponse> HandleAsync(CreatePolicyCommand command, CancellationToken cancellationToken)
    {
        var policyTypeDefinition = await _policyTypeDefinitionRepository.GetApplicableAsync(
            command.PolicyName,
            command.PolicyTypeEffectiveFrom,
            cancellationToken);

        var vehicle = Validate(command, policyTypeDefinition);

        var vehicleDetails = new Vehicle(
            vehicle.RegistrationNumber,
            vehicle.Model,
            vehicle.VehicleType);

        var policy = Policy.Create(
            policyTypeDefinition!.Id,
            command.CustomerId,
            command.EffectiveDate,
            command.ExpirationDate,
            vehicleDetails);

        await _policyRepository.AddAsync(policy, cancellationToken);
        await _policyUnitOfWork.CommitAsync(cancellationToken);
        
        return new CreatePolicyResponse(policy.Id);
    }

    private VehicleDetailsDto Validate(
        CreatePolicyCommand command,
        PolicyTypeDefinition? policyTypeDefinition)
    {
        if (policyTypeDefinition == null)
        {
            throw new ArgumentException($"Policy type definition '{command.PolicyName}' effective from {command.PolicyTypeEffectiveFrom:O} does not exist.");
        }
        if(command.Vehicle == null)
        {
            throw new ArgumentException("Vehicle details must be provided.");
        }
        if(string.IsNullOrWhiteSpace(command.Vehicle.RegistrationNumber))
        {
            throw new ArgumentException("Vehicle registration number must be provided.");
        }
        if(string.IsNullOrWhiteSpace(command.Vehicle.Model))
        {
            throw new ArgumentException("Vehicle model must be provided.");
        }
        if(!Enum.IsDefined(typeof(VehicleType), command.Vehicle.VehicleType))
        {
            throw new ArgumentException("Invalid vehicle type.");
        }
        if(policyTypeDefinition.AllowedVehicleTypes.Contains(command.Vehicle.VehicleType) == false)
        {
            throw new ArgumentException($"Vehicle type {command.Vehicle.VehicleType} is not allowed for policy type {policyTypeDefinition.Name}.");
        }

        return command.Vehicle;
    }
}
