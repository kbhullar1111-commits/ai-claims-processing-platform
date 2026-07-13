using Deployment.Platform.Application.Interfaces.Execution;
using Deployment.Platform.Application.Models.Execution;
using Deployment.Platform.Domain.Execution;

namespace Deployment.Platform.Application.Services.Execution;

public sealed class StageExecutor : IStageExecutor
{
    private readonly IArtifactExecutor _artifactExecutor;

    public StageExecutor(IArtifactExecutor artifactExecutor)
    {
        _artifactExecutor = artifactExecutor;
    }
    public async  Task<StageExecutionResult> ExecuteAsync(
        ExecutionStage stage,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;

        var artifactResults  = new List<ArtifactExecutionResult>();   
        foreach(var artifact in stage.Artifacts)
        {
          cancellationToken.ThrowIfCancellationRequested();
          var artifactResult =   await _artifactExecutor.ExecuteAsync(artifact, cancellationToken);
          artifactResults.Add(artifactResult);
          if (!artifactResult.Successful)
          {
            break;
          }
        }

        var completedAt = DateTime.UtcNow;
        var completed = artifactResults.Count == stage.Artifacts.Count;

        return new StageExecutionResult
        {
            StartedAt = startedAt,
            CompletedAt = completedAt,
            StageOrder = stage.Order,
            Successful = completed && artifactResults.All(ar => ar.Successful),
            ArtifactResults = artifactResults
        }; 
    }
}