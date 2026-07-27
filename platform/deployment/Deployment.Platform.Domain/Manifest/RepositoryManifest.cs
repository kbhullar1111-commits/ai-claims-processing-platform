
namespace Deployment.Platform.Domain.Manifest;

public sealed class RepositoryManifest
{
    public int Version { get; init; }

    public RepositoryInfo? Repository { get; init; }

    public List<ArtifactDefinition> Artifacts { get; init; } = [];
}