using Deployment.Platform.Domain.Planning;
using Deployment.Platform.Domain.Execution;
using Deployment.Platform.Domain.Manifest;

namespace Deployment.Platform.Application.Interfaces;

public interface IExecutionGraphBuilder
{
    ExecutionGraph Build(
        DeploymentPlan deploymentPlan,
        RepositoryManifest manifest);
}