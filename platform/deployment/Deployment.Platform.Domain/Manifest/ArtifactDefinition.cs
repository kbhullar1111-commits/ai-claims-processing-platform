namespace Deployment.Platform.Domain.Manifest;

using Deployment.Platform.Domain.Artifacts;

public sealed class ArtifactDefinition
{
    public required string Name { get; init; }

    public required ArtifactType Type { get; init; }

    public required string Project { get; init; }

    public string? Dockerfile { get; init; }

    public List<string> Dependencies { get; init; } = [];
}