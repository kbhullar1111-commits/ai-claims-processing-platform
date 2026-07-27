using Deployment.Platform.Domain.Execution;

namespace Deployment.Platform.Application.Models.Execution;

public sealed class DeploymentExecutionRequest
{
    public required ExecutionGraph ExecutionGraph { get; init;}

    public required DeploymentEnvironment DeploymentEnvironment { get; init;}
    
    public required string ImageTag { get; init; }

    public bool DryRun { get; init; }

    public bool AutoApprove { get; init; }
}