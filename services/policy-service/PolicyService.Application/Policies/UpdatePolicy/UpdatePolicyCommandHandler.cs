using PolicyService.Domain.Enums;
using PolicyService.Domain.Entities;
using PolicyService.Application.Abstractions;

namespace PolicyService.Application.Policies;

public sealed class UpdatePolicyCommandHandler
{
    private readonly IPolicyRepository _policyRepository;

    private readonly IPolicyUnitOfWork _policyUnitOfWork;

    public UpdatePolicyCommandHandler(IPolicyRepository policyRepository, IPolicyUnitOfWork policyUnitOfWork)
    {
        _policyRepository = policyRepository;
        _policyUnitOfWork = policyUnitOfWork;
    }

    public async Task<UpdatePolicyResponse> HandleAsync(UpdatePolicyCommand command, CancellationToken cancellationToken)
    {
        var policy = await _policyRepository.GetByIdForUpdateAsync(command.PolicyId, cancellationToken);
        if (policy == null)
        {
            throw new InvalidOperationException($"Policy with ID {command.PolicyId} not found.");
        }

        // Update the policy status
        if (command.Status == PolicyStatus.Cancelled)
        {
            policy.Cancel();
        }

        await _policyUnitOfWork.CommitAsync(cancellationToken);

        return new UpdatePolicyResponse("Policy updated successfully.");
    }
}