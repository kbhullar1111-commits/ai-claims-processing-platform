using Deployment.Platform.Application.Interfaces.Execution;
using Deployment.Platform.Application.Models.Execution;

namespace Deployment.Platform.Application.Services.Execution;

public sealed class DeploymentExecutor : IDeploymentExecutor
{
    private readonly IStageExecutor _stageExecutor;

    public DeploymentExecutor(IStageExecutor stageExecutor)
    {
        _stageExecutor = stageExecutor;
    }
    public async Task<DeploymentExecutionResult> ExecuteAsync(
        DeploymentExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;

        var stageResults  = new List<StageExecutionResult>();   
        foreach(var stage in request.ExecutionGraph.Stages)
        {
          cancellationToken.ThrowIfCancellationRequested();
          var stageResult =   await _stageExecutor.ExecuteAsync(stage, cancellationToken);
          stageResults .Add(stageResult);
            if (!stageResult.Successful)
            {
                break;
            }
        }

        var completedAt = DateTime.UtcNow;
        var completed = stageResults.Count == request.ExecutionGraph.Stages.Count;

        return new DeploymentExecutionResult
        {
            StartedAt = startedAt,
            CompletedAt = completedAt,
            Successful = completed && stageResults .All(sr => sr.Successful),
            StageResults = stageResults
        }; 
    }
}