using Deployment.Platform.Application.Models;
using Deployment.Platform.Application.Services;
using Deployment.Platform.Domain.Artifacts;
using Deployment.Platform.Domain.Impact;
using Deployment.Platform.Domain.Manifest;
using Deployment.Platform.Domain.Planning;
using Deployment.Platform.Domain.Execution;

namespace Deployment.Platform.Application.Tests;

public class DeploymentPlannerTests
{
    private readonly DeploymentPlanner _planner = new();
    private readonly ExecutionGraphBuilder _graphBuilder = new();

    [Fact]
    public void CreatePlan_WithUnknownSelectedArtifact_ThrowsInvalidOperationException()
    {
        var request = CreateRequest(DeploymentStrategy.Selected, ["claims-service", "gateway-service"], ["claims-service"], ["missing-service"]);

        var exception = Assert.Throws<InvalidOperationException>(() => _planner.CreatePlan(request));

        Assert.Contains("missing-service", exception.Message);
    }
    
    [Fact]
    public void Build_WithDeploymentPlan_CreatesExecutionGraph()
    {
        var request = CreateRequest(DeploymentStrategy.Impacted, ["claims-service"], ["claims-service"], null);

        var plan = _planner.CreatePlan(request);
        var executionGraph = _graphBuilder.Build(plan);

        Assert.NotNull(executionGraph);
        Assert.Single(executionGraph.Stages);
        Assert.Single(executionGraph.Stages.First().Artifacts);
        Assert.Equal("claims-service", executionGraph.Stages.First().Artifacts.First().Artifact.Name);
    }

    [Fact]
    public void Build_PreservesArtifactOrder()
    {
        var request = CreateRequest(DeploymentStrategy.Impacted, ["claims-service", "gateway-service"], ["claims-service", "gateway-service"], null);

        var plan = _planner.CreatePlan(request);
        var executionGraph = _graphBuilder.Build(plan);

        Assert.NotNull(executionGraph);
        Assert.Single(executionGraph.Stages);
        Assert.Equal(2, executionGraph.Stages.First().Artifacts.Count);
        Assert.Equal("claims-service", executionGraph.Stages.First().Artifacts.ElementAt(0).Artifact.Name);
        Assert.Equal("gateway-service", executionGraph.Stages.First().Artifacts.ElementAt(1).Artifact.Name);
    }

    [Fact]
    public void Build_WithEmptyDeploymentPlan_Throws()
    {
        var request = CreateRequest(DeploymentStrategy.Impacted, [], null, null);

        var plan = _planner.CreatePlan(request);

        Assert.Throws<ArgumentException>(() => _graphBuilder.Build(plan));
    }

    private static DeploymentPlanRequest CreateRequest(DeploymentStrategy strategy, string[] artifactNames, ICollection<string>? impactedArtifactNames, ICollection<string>? selectedArtifacts)
    {
        var artifacts = artifactNames.Select(name => CreateArtifact(name)).ToList();

        return new DeploymentPlanRequest
        {
            Manifest = new RepositoryManifest
            {
                Artifacts = artifacts
            },
            ImpactAnalysis = impactedArtifactNames != null && impactedArtifactNames.Any() 
            ? new ImpactAnalysisResult
            {
                Artifacts = artifacts.Where(artifact => impactedArtifactNames.Contains(artifact.Name))
                .Select(artifact => new ImpactedArtifact
                {
                    Artifact = artifact,
                    ImpactType = ImpactType.Direct
                }).ToList()
            } : new ImpactAnalysisResult(),
            Strategy = strategy,
            SelectedArtifacts = selectedArtifacts ?? []
        };
    }

    private static ArtifactDefinition CreateArtifact(string name)
    {
        return new ArtifactDefinition
        {
            Name = name,
            Type = ArtifactType.Api,
            Root = $"/src/{name}",
            EntryPoint = "Program.cs"
        };
    }
}
