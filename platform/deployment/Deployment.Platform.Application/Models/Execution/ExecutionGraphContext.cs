using Deployment.Platform.Domain.Manifest;

namespace Deployment.Platform.Application.Models;

internal sealed class ExecutionGraphContext
{
    public required HashSet<string> PlannedArtifacts { get; init; }

    public required Dictionary<string, int> RemainingDependencyCount { get; init; }

    public required Dictionary<string, List<string>> Dependents { get; init; }

}