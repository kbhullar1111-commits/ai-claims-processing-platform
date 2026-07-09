using Deployment.Platform.Application.Interfaces;
using Deployment.Platform.Application.Models;
using Deployment.Platform.Domain.Planning;
using Deployment.Platform.Domain.Execution;
using Deployment.Platform.Domain.Manifest;

namespace Deployment.Platform.Application.Services;

public sealed class ExecutionGraphBuilder : IExecutionGraphBuilder
{
    public ExecutionGraph Build(
        DeploymentPlan deploymentPlan,
        RepositoryManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(deploymentPlan);
        ArgumentNullException.ThrowIfNull(manifest);

        var context = BuildContext(deploymentPlan,manifest);

        var executionStages = BuildExecutionStages(
            context,
            deploymentPlan);

        return ExecutionGraph.Create(executionStages);

    }

    private static ExecutionGraphContext BuildContext(
        DeploymentPlan deploymentPlan,
        RepositoryManifest manifest)
    {

        var plannedArtifacts = deploymentPlan.Artifacts
            .Select(artifact => artifact.Artifact.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var remainingDependencyCount = plannedArtifacts
            .ToDictionary(
                artifact => artifact,
                _ => 0,
                StringComparer.OrdinalIgnoreCase);

        var dependents = plannedArtifacts
            .ToDictionary(
                artifact => artifact,
                _ => new List<string>(),
                StringComparer.OrdinalIgnoreCase);
                

        foreach (var plannedArtifact in deploymentPlan.Artifacts)
        {
            foreach (var dependency in plannedArtifact.Artifact.Dependencies)
            {
                var dependencyArtifact = ResolveArtifact(dependency, manifest);

                if (dependencyArtifact is null)
                {
                    continue;
                }

                if (!plannedArtifacts.Contains(dependencyArtifact.Name))
                {
                    continue;
                }

                remainingDependencyCount[plannedArtifact.Artifact.Name]++;
                dependents[dependencyArtifact.Name].Add(plannedArtifact.Artifact.Name);
            }
        }
        // Populate dependency graph

        return new ExecutionGraphContext
        {
            PlannedArtifacts = plannedArtifacts,
            RemainingDependencyCount = remainingDependencyCount,
            Dependents = dependents
        };
    }

    private static IReadOnlyCollection<ExecutionStage> BuildExecutionStages(
        ExecutionGraphContext context,
        DeploymentPlan deploymentPlan)
    {
        var executionStages = new List<ExecutionStage>();
        var remainingArtifacts = new HashSet<string>(context.PlannedArtifacts, StringComparer.OrdinalIgnoreCase);
        var plannedArtifactsByName =
                deploymentPlan.Artifacts.ToDictionary(
                    a => a.Artifact.Name,
                    StringComparer.OrdinalIgnoreCase);

        while (remainingArtifacts.Count > 0)
        {
            var readyArtifacts = remainingArtifacts
                .Where(artifact => context.RemainingDependencyCount[artifact] == 0)
                .ToList();

            if (readyArtifacts.Count == 0)
            {
                throw new InvalidOperationException(
                        $"Circular dependency detected among: {string.Join(", ", remainingArtifacts)}");
            }

            var executionArtifacts = CreateExecutionArtifacts(plannedArtifactsByName, readyArtifacts);
            executionStages.Add(ExecutionStage.Create(executionStages.Count + 1, executionArtifacts));

            foreach (var artifact in readyArtifacts)
            {
                remainingArtifacts.Remove(artifact);

                foreach (var dependent in context.Dependents[artifact])
                {
                    context.RemainingDependencyCount[dependent]--;
                }
            }
        }

        return executionStages;
    }

    private static ArtifactDefinition? ResolveArtifact(
        string dependency,
        RepositoryManifest manifest)
    {
        return manifest.Artifacts
            .Where(a =>
                dependency.Equals(
                    a.Root,
                    StringComparison.OrdinalIgnoreCase) ||
                dependency.StartsWith(
                    a.Root + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(a => a.Root.Length)
            .FirstOrDefault();
    }

    private static IReadOnlyCollection<ExecutionArtifact> CreateExecutionArtifacts(
        Dictionary<string, PlannedArtifact> plannedArtifacts,
        IReadOnlyCollection<string> readyArtifacts)
    {
        return readyArtifacts
            .Select(name =>
                ExecutionArtifact.Create(
                    plannedArtifacts[name].Artifact))
            .ToList();
    }

}
