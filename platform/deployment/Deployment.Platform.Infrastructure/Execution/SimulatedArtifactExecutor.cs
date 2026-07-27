using Deployment.Platform.Application.Interfaces.Execution;
using Deployment.Platform.Application.Models.Execution;
using Deployment.Platform.Domain.Execution;


namespace Deployment.Platform.Infrastructure.Execution;

public sealed class SimulatedArtifactExecutor : IArtifactExecutor
{
    public async Task<ArtifactExecutionResult> ExecuteAsync(
        ExecutionArtifact artifact,
        string imageTag,
        DeploymentEnvironment deploymentEnvironment,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var startedAt = DateTime.UtcNow;
        Console.WriteLine($"Deploying {artifact.Artifact.Name}...");
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        var completedAt = DateTime.UtcNow;
        return new ArtifactExecutionResult
        {
            ArtifactName = artifact.Artifact.Name,

            StartedAt = startedAt,

            CompletedAt = completedAt,

            Successful = true,

            ErrorMessage = null
        };
    }

}
