using Deployment.Platform.Application.Models;

namespace Deployment.Platform.Application.Interfaces;

public interface IDeploymentExecutor
{
    Task<DeploymentExecutionResult> ExecuteAsync(
        DeploymentExecutionRequest request,
        CancellationToken cancellationToken = default);
}