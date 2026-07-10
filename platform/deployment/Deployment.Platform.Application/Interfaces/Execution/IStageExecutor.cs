using Deployment.Platform.Application.Models;
using Deployment.Platform.Domain.Execution;

namespace Deployment.Platform.Application.Interfaces;

public interface IStageExecutor
{
    Task<StageExecutionResult> ExecuteAsync(
        ExecutionStage stage,
        CancellationToken cancellationToken = default);
}