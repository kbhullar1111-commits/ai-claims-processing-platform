using Deployment.Platform.Application.Models.Execution;

namespace Deployment.Platform.Application.Interfaces.Validation;

public interface IDeploymentTargetValidator
{
    Task ValidateAsync(
        DeploymentEnvironment deploymentEnvironment,
        CancellationToken cancellationToken);
}