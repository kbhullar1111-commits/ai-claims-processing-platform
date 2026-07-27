namespace Deployment.Platform.Domain.Impact;
public sealed class ImpactAnalysisResult
{
    public List<ImpactedArtifact> Artifacts { get; init; } = [];
}