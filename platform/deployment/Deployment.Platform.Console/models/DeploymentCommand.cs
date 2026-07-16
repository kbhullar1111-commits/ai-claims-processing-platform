using Deployment.Platform.Domain.Planning;

public sealed class DeploymentCommand
{
    public required DeploymentStrategy Strategy { get; init; }

    public required DeploymentTarget Target { get; init; }

    public required string Environment { get; init; }

    public IReadOnlyList<string> SelectedArtifacts { get; init; } = [];

    public bool DryRun { get; init; }

    public bool AutoApprove { get; init; }
}

public enum DeploymentTarget
{
    AzureContainerApps
}