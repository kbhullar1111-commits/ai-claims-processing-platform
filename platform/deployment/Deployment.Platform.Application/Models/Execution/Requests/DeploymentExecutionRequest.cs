using Deployment.Platform.Domain.Execution;

namespace Deployment.Platform.Application.Models;

public sealed class DeploymentExecutionRequest
{
    public required ExecutionGraph ExecutionGraph { get; init;}

    public required string Environment { get; init;}

    public bool DryRun { get; init; }

    public bool AutoApprove { get; init; }
}