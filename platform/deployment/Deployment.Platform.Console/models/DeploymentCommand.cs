using Deployment.Platform.Domain.Planning;

public sealed class DeploymentCommand
{
    public required DeploymentStrategy Strategy { get; init; }

    public required DeploymentTarget Target { get; init; }

    public required string Environment { get; init; }

    public required string ManifestPath { get; init; }

    public required string SettingsPath { get; init; }

    public IReadOnlyList<string> SelectedArtifacts { get; init; } = [];

    public bool DryRun { get; init; }

    public bool AutoApprove { get; init; }

    public string? BaseCommit { get; init; }

    public string? HeadCommit { get; init; }

}

public enum DeploymentTarget
{
    AzureContainerApps
}