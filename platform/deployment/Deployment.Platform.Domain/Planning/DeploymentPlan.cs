using Deployment.Platform.Domain.Manifest;

namespace Deployment.Platform.Domain.Planning;

public sealed class DeploymentPlan
{
    public required DeploymentStrategy Strategy { get; init; }

    public List<PlannedArtifact> Artifacts { get; init; } = [];

    public static DeploymentPlan Create(
        DeploymentStrategy strategy,
        List<PlannedArtifact> artifacts)
    {
        return new DeploymentPlan
        {
            Strategy = strategy,
            Artifacts = artifacts
        };
    }
}