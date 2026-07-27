namespace Deployment.Platform.Domain.Manifest;

using Deployment.Platform.Domain.Artifacts;

public sealed class ArtifactDefinition
{
    public required string Name { get; init; }

    public required ArtifactType Type { get; init; }

    public required string Root { get; init; }

    public required string EntryPoint { get; init; }

    public string? Dockerfile { get; init; }

    public ICollection<string> Dependencies { get; init; } = [];

    public ICollection<string> IgnoredPaths { get; init; } = [];
}