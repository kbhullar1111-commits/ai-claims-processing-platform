using Deployment.Platform.Application.Interfaces.Execution;
using Deployment.Platform.Application.Interfaces.Validation;
using Deployment.Platform.Application.Models.Execution;

namespace Deployment.Platform.Application.Services.Execution;

public sealed class DeploymentExecutor : IDeploymentExecutor
{
    private readonly IStageExecutor _stageExecutor;
    private readonly IExecutionEnvironmentValidator _executionEnvironmentValidator;
    private readonly IDeploymentTargetValidator _deploymentEnvironmentValidator;

    public DeploymentExecutor(
        IStageExecutor stageExecutor,
        IExecutionEnvironmentValidator executionEnvironmentValidator,
        IDeploymentTargetValidator deploymentEnvironmentValidator)
    {
        _stageExecutor = stageExecutor;
        _executionEnvironmentValidator = executionEnvironmentValidator;
        _deploymentEnvironmentValidator = deploymentEnvironmentValidator;
    }
    public async Task<DeploymentExecutionResult> ExecuteAsync(
        DeploymentExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        var deploymentEnvironment = request.DeploymentEnvironment;
        var startedAt = DateTime.UtcNow;

        await _executionEnvironmentValidator.ValidateAsync(cancellationToken);
        await _deploymentEnvironmentValidator.ValidateAsync(deploymentEnvironment, cancellationToken);

        var stageResults  = new List<StageExecutionResult>();   

        foreach(var stage in request.ExecutionGraph.Stages)
        {
          cancellationToken.ThrowIfCancellationRequested();
          var stageResult =   await _stageExecutor.ExecuteAsync(
            stage,
            deploymentEnvironment,
            cancellationToken);

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