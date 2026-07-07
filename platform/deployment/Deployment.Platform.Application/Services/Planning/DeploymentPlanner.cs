
using Deployment.Platform.Application.Interfaces;
using Deployment.Platform.Application.Models;
using Deployment.Platform.Domain.Planning;
using Deployment.Platform.Domain.Impact;
using Deployment.Platform.Domain.Manifest;

namespace Deployment.Platform.Application.Services;

public sealed class DeploymentPlanner : IDeploymentPlanner
{
    public DeploymentPlan CreatePlan(
        DeploymentPlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Strategy switch
        {
            DeploymentStrategy.Impacted =>
                CreateImpactedPlan(request),

            DeploymentStrategy.Selected =>
                CreateSelectedPlan(request),

            DeploymentStrategy.Full =>
                CreateFullPlan(request),

            _ => throw new InvalidOperationException(
                $"Unknown deployment strategy '{request.Strategy}'.")
        };
    }

    private DeploymentPlan CreateImpactedPlan(
        DeploymentPlanRequest request)
    {
        var impactedArtifacts = request.ImpactAnalysis.Artifacts
            .Select(a => PlannedArtifact.Create(a.Artifact))
            .ToList();

        return DeploymentPlan.Create(request.Strategy, impactedArtifacts);
    }

    private DeploymentPlan CreateSelectedPlan(
        DeploymentPlanRequest request)
    {
        var artifacts = request.Manifest.Artifacts
        .ToDictionary(
            a => a.Name,
            StringComparer.OrdinalIgnoreCase);

        var unknownSelectedArtifacts = request.SelectedArtifacts
            .Where(name => !artifacts.ContainsKey(name))
            .ToList();

        if (unknownSelectedArtifacts.Count > 0)
        {
            throw new InvalidOperationException(
                $"Selected artifacts were not found in the manifest: {string.Join(", ", unknownSelectedArtifacts)}");
        }

        var selectedArtifacts = request.SelectedArtifacts
        .Select(name => PlannedArtifact.Create(artifacts[name]))
        .ToList();

        return DeploymentPlan.Create(request.Strategy, selectedArtifacts);
    }

    private DeploymentPlan CreateFullPlan(
        DeploymentPlanRequest request)
    {
         var artifacts = request.Manifest.Artifacts
        .Select(a => PlannedArtifact.Create(a))
        .ToList();

        return DeploymentPlan.Create(request.Strategy, artifacts);
    }
}