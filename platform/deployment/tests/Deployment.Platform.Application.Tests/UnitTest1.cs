using Deployment.Platform.Application.Models;
using Deployment.Platform.Application.Services;
using Deployment.Platform.Domain.Artifacts;
using Deployment.Platform.Domain.Impact;
using Deployment.Platform.Domain.Manifest;
using Deployment.Platform.Domain.Planning;

namespace Deployment.Platform.Application.Tests;

public class DeploymentPlannerTests
{
    [Fact]
    public void CreatePlan_WithUnknownSelectedArtifact_ThrowsInvalidOperationException()
    {
        var planner = new DeploymentPlanner();
        var request = new DeploymentPlanRequest
        {
            Manifest = new RepositoryManifest
            {
                Artifacts =
                [
                    new ArtifactDefinition
                    {
                        Name = "claims-service",
                        Type = ArtifactType.Api,
                        Root = "/src/claims",
                        EntryPoint = "Program.cs"
                    }
                ]
            },
            ImpactAnalysis = new ImpactAnalysisResult(),
            Strategy = DeploymentStrategy.Selected,
            SelectedArtifacts = ["missing-service"]
        };

        var exception = Assert.Throws<InvalidOperationException>(() => planner.CreatePlan(request));

        Assert.Contains("missing-service", exception.Message);
    }
}
