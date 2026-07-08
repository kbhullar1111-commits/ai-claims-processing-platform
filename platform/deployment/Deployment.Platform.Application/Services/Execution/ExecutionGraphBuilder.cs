using Deployment.Platform.Application.Interfaces;
using Deployment.Platform.Domain.Planning;
using Deployment.Platform.Domain.Execution;

namespace Deployment.Platform.Application.Services;

public sealed class ExecutionGraphBuilder : IExecutionGraphBuilder
{
    public ExecutionGraph Build(
        DeploymentPlan deploymentPlan)
    {
        ArgumentNullException.ThrowIfNull(deploymentPlan);

        var artifacts = deploymentPlan.Artifacts
            .Select(artifact => ExecutionArtifact.Create(artifact.Artifact))
            .ToList();

        var executionStage = ExecutionStage.Create(1, artifacts);

        return ExecutionGraph.Create(new List<ExecutionStage> { executionStage });
    }
}
