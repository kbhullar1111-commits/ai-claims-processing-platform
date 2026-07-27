using Deployment.Platform.Domain.Manifest;

namespace Deployment.Platform.Domain.Planning;

public sealed class PlannedArtifact
{
    public required ArtifactDefinition Artifact { get; init; }

    public static PlannedArtifact Create(ArtifactDefinition artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        return new PlannedArtifact
        {
            Artifact = artifact
        };
    }
    
}