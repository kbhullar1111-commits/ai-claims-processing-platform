using Deployment.Platform.Domain.Impact;
using Deployment.Platform.Domain.Manifest;
using Deployment.Platform.Domain.Planning;
using Deployment.Platform.Application.Models.Planning;

namespace Deployment.Platform.Application.Interfaces.Planning;

public interface IDeploymentPlanner
{
    DeploymentPlan CreatePlan(
        DeploymentPlanRequest request);
}