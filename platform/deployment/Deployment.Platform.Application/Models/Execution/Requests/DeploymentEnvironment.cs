namespace Deployment.Platform.Application.Models.Execution;

public sealed class DeploymentEnvironment
{
    public required string Name { get; init; }

    public required string ResourceGroup { get; init; }

    public required string AcrName { get; init; }

    public required string AcrServer { get; init; }

    public required string ContainerAppEnvironment { get; init; }

    public required string ImageTag { get; init; }
}