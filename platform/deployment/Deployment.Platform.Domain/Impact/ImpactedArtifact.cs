using Deployment.Platform.Domain.Manifest;
using Deployment.Platform.Domain.Changes;

namespace Deployment.Platform.Domain.Impact;

public sealed class ImpactedArtifact
{
    public required ArtifactDefinition Artifact { get; init; }

    public required ImpactType ImpactType { get; init; }

    public List<ChangedFile> ChangedFiles { get; init; } = [];
}