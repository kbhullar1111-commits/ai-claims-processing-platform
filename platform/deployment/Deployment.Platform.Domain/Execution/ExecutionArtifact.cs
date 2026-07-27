using Deployment.Platform.Domain.Manifest;

namespace Deployment.Platform.Domain.Execution;

public sealed class ExecutionArtifact
{
    private ExecutionArtifact(ArtifactDefinition artifact)
    {
        Artifact = artifact;
    }
    public ArtifactDefinition Artifact { get; }

    public static ExecutionArtifact Create(ArtifactDefinition artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        return new ExecutionArtifact(artifact);
    }

}