using Deployment.Platform.Application.Models.Execution;
using Deployment.Platform.Domain.Execution;

namespace Deployment.Platform.Application.Interfaces.Execution;

public interface IArtifactExecutor
{
    Task<ArtifactExecutionResult> ExecuteAsync(
        ExecutionArtifact artifact,
        string imageTag,
        DeploymentEnvironment deploymentEnvironment,
        CancellationToken cancellationToken = default);
}