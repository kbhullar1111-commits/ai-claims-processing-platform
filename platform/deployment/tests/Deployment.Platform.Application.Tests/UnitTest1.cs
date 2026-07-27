using Deployment.Platform.Application.Models.Planning;
using Deployment.Platform.Application.Models.Execution;
using Deployment.Platform.Application.Services.Impact;
using Deployment.Platform.Application.Services.Planning;
using Deployment.Platform.Application.Services.Execution;
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
    public void Build_ExecutionGraph_With_SingleArtifact()
    {
        var request = CreateRequest(DeploymentStrategy.Impacted, ["gateway-service"], ["gateway-service"], null);

        var plan = _planner.CreatePlan(request);
        var repositoryManifest = GetRepositoryManifest();
        var executionGraph = _graphBuilder.Build(plan, repositoryManifest);

        Assert.NotNull(executionGraph);
        Assert.Single(executionGraph.Stages);
        Assert.Equal("gateway-service", executionGraph.Stages.First().Artifacts.ElementAt(0).Artifact.Name);
    }
    
    [Fact]
    public void Build_ExecutionGraph_With_MultipleArtifacts()
    {
        var request = CreateRequest(DeploymentStrategy.Impacted, ["claims-service","gateway-service"], ["claims-service","gateway-service"], null);

        var plan = _planner.CreatePlan(request);
        var repositoryManifest = GetRepositoryManifest();

        var executionGraph = _graphBuilder.Build(plan, repositoryManifest);

        Assert.NotNull(executionGraph);
        Assert.Equal(2, executionGraph.Stages.Count());
        Assert.Equal("claims-service", executionGraph.Stages.First().Artifacts.ElementAt(0).Artifact.Name);
        Assert.Equal("gateway-service", executionGraph.Stages.Skip(1).First().Artifacts.ElementAt(0).Artifact.Name);
    }

    [Fact]
    public void Build_ParallelExecutionStages()
    {
        var request = CreateRequest(DeploymentStrategy.Impacted, ["claims-service", "gateway-service", "document-service"], ["claims-service", "gateway-service", "document-service"], null);

        var plan = _planner.CreatePlan(request);
        var repositoryManifest = GetRepositoryManifest();
        var executionGraph = _graphBuilder.Build(plan, repositoryManifest);

        Assert.NotNull(executionGraph);
        Assert.Equal(2, executionGraph.Stages.Count());
        Assert.Equal(2, executionGraph.Stages.First().Artifacts.Count);
        Assert.True(executionGraph.Stages.First().Artifacts.Select(a => a.Artifact.Name).Contains("claims-service"));
        Assert.True(executionGraph.Stages.First().Artifacts.Select(a => a.Artifact.Name).Contains("document-service"));
        Assert.Equal("gateway-service", executionGraph.Stages.Skip(1).First().Artifacts.ElementAt(0).Artifact.Name);
    }

    [Fact]
    public void Build_WithEmptyDeploymentPlan_Throws()
    {
        var request = CreateRequest(DeploymentStrategy.Impacted, [], null, null);

        var plan = _planner.CreatePlan(request);

        var repositoryManifest = GetRepositoryManifest();

        Assert.Throws<ArgumentException>(() => _graphBuilder.Build(plan, repositoryManifest));
    }

    // [Fact]
    // public void Build_ExecutionGraph_With_Circular_Dependencies_Throws()
    // {
    //     var request = CreateRequest(DeploymentStrategy.Impacted, ["claims-service", "gateway-service"], ["claims-service", "gateway-service"], null);

    //     var plan = _planner.CreatePlan(request);
    //     var repositoryManifest = GetRepositoryManifest();

    //     // Manually introduce a circular dependency for testing
    //     var claimsService = repositoryManifest.Artifacts.First(a => a.Name == "claims-service");
    //     claimsService.Dependencies.Add(new ArtifactDependency { Name = "gateway-service" });

    //     var gatewayService = repositoryManifest.Artifacts.First(a => a.Name == "gateway-service");
    //     gatewayService.Dependencies.Add(new ArtifactDependency { Name = "claims-service" });

    //     Assert.Throws<InvalidOperationException>(() => _graphBuilder.Build(plan, repositoryManifest));
    // }

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
            Root = $"services/{name}",
            EntryPoint = "Program.cs",
            Dependencies = name == "gateway-service" ? new List<string>
                {
                    "services/claims-service/ClaimsService.API",
                    "services/document-service/DocumentService.API"
                } : new List<string>{
                    "building-blocks/contracts"
                },
            IgnoredPaths = []
            
        };
    }

    private static RepositoryManifest GetRepositoryManifest()
    {
        IEnumerable<string> artifactNames = new List<string>
        {
            "claims-service",
            "gateway-service",
            "document-service",
            "notification-service",
            "fraud-service",
            "payment-service",
            "document-processor"
        };
        var artifacts = artifactNames.Select(name => CreateArtifact(name)).ToList();

        return new RepositoryManifest
        {
            Repository = new RepositoryInfo
            {
                Name = "my-repo"
            },
            Artifacts = artifacts
        };
    }
}
