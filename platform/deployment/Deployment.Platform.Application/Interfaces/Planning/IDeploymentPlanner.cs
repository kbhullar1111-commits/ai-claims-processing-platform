using Deployment.Platform.Domain.Impact;
using Deployment.Platform.Domain.Manifest;
using Deployment.Platform.Domain.Planning;
using Deployment.Platform.Application.Models;

namespace Deployment.Platform.Application.Interfaces;

public interface IDeploymentPlanner
{
    DeploymentPlan CreatePlan(
        DeploymentPlanRequest request);
}