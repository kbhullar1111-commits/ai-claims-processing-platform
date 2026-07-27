using Deployment.Platform.Application.Models.Execution;

namespace Deployment.Platform.Application.Interfaces.Execution;

public interface IDeploymentExecutor
{
    Task<DeploymentExecutionResult> ExecuteAsync(
        DeploymentExecutionRequest request,
        CancellationToken cancellationToken = default);
}