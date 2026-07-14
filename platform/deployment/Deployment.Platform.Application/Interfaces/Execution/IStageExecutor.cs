using Deployment.Platform.Application.Models.Execution;
using Deployment.Platform.Domain.Execution;

namespace Deployment.Platform.Application.Interfaces.Execution;

public interface IStageExecutor
{
    Task<StageExecutionResult> ExecuteAsync(
        ExecutionStage stage,
        DeploymentEnvironment deploymentEnvironment,
        CancellationToken cancellationToken = default);
}