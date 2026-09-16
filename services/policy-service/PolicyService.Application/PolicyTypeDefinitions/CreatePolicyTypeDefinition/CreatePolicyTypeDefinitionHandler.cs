using PolicyService.Application.Abstractions;
using PolicyService.Domain.Entities;
using PolicyService.Domain.Enums;

namespace PolicyService.Application.PolicyTypeDefinitions;

public sealed class CreatePolicyTypeDefinitionHandler
{
    private readonly IPolicyTypeDefinitionRepository _policyTypeDefinitionRepository;
    private readonly IPolicyUnitOfWork _policyUnitOfWork;

    public CreatePolicyTypeDefinitionHandler(
        IPolicyTypeDefinitionRepository policyTypeDefinitionRepository,
        IPolicyUnitOfWork policyUnitOfWork)
    {
        _policyTypeDefinitionRepository = policyTypeDefinitionRepository;
        _policyUnitOfWork = policyUnitOfWork;
    }

    public async Task<CreatePolicyTypeDefinitionResponse> HandleAsync(
        CreatePolicyTypeDefinitionCommand command,
        CancellationToken cancellationToken)
    {
        Validate(command);

        var coverages = command.Coverages.Select(c => new Coverage(c.Type, c.CoverageLimit)).ToList();

        var policyTypeDefinition = PolicyTypeDefinition.Create(
            command.Type,
            command.Name.Trim(),
            command.EffectiveFrom,
            command.EffectiveTo,
            coverages,
            command.MaximumClaimAmount,
            command.AllowedVehicleTypes);

        await _policyTypeDefinitionRepository.AddAsync(policyTypeDefinition, cancellationToken);
        await _policyUnitOfWork.CommitAsync(cancellationToken);

        return new CreatePolicyTypeDefinitionResponse(policyTypeDefinition.Id);
    }

    private static void Validate(CreatePolicyTypeDefinitionCommand command)
    {
        if (!Enum.IsDefined(command.Type))
        {
            throw new ArgumentException("Invalid policy type.");
        }

        if (command.Coverages == null || command.Coverages.Count == 0)
        {
            throw new ArgumentException("At least one coverage must be provided.");
        }

        foreach (var coverage in command.Coverages)
        {
            if (!Enum.IsDefined(coverage.Type))
            {
                throw new ArgumentException("Invalid coverage type.");
            }

            if (coverage.CoverageLimit <= 0)
            {
                throw new ArgumentException("Coverage limit must be greater than zero.");
            }
        }

        if (command.AllowedVehicleTypes == null || command.AllowedVehicleTypes.Count == 0)
        {
            throw new ArgumentException("At least one allowed vehicle type must be provided.");
        }

        if (command.AllowedVehicleTypes.Any(vehicleType => !Enum.IsDefined(vehicleType)))
        {
            throw new ArgumentException("Invalid allowed vehicle type.");
        }
    }
}