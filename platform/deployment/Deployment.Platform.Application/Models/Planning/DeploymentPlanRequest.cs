using Deployment.Platform.Domain.Manifest;
using Deployment.Platform.Domain.Impact;
using Deployment.Platform.Domain.Planning;

namespace Deployment.Platform.Application.Models;

public sealed class DeploymentPlanRequest
{
    public required RepositoryManifest Manifest { get; init; }

    public required ImpactAnalysisResult ImpactAnalysis { get; init; }

    public required DeploymentStrategy Strategy { get; init; }

    public ICollection<string> SelectedArtifacts { get; init; } = [];
}