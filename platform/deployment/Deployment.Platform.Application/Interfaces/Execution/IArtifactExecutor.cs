using Deployment.Platform.Application.Models;
using Deployment.Platform.Domain.Execution;

namespace Deployment.Platform.Application.Interfaces;

public interface IArtifactExecutor
{
    Task<ArtifactExecutionResult> ExecuteAsync(
        ExecutionArtifact artifact,
        CancellationToken cancellationToken = default);
}