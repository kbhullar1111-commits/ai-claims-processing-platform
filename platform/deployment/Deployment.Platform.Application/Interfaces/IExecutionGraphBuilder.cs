using Deployment.Platform.Domain.Planning;
using Deployment.Platform.Domain.Execution;

namespace Deployment.Platform.Application.Interfaces;

public interface IExecutionGraphBuilder
{
    ExecutionGraph Build(
        DeploymentPlan deploymentPlan);
}