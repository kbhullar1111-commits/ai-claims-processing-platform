namespace Deployment.Platform.Application.Models.Execution;

public sealed class DeploymentEnvironment
{
    public required string Name { get; init; }

    public required string ResourceGroup { get; init; }

    public required string ContainerRegistryName { get; init; }

    public required string ContainerRegistryServer { get; init; }

    public required string ContainerAppEnvironment { get; init; }

    public required string ImageTag { get; init; }
}