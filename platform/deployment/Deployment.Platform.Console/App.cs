using Deployment.Platform.Application.Interfaces;
using Deployment.Platform.Application.Models;
using Deployment.Platform.Domain.Changes;
using Deployment.Platform.Domain.Impact;
using Deployment.Platform.Domain.Manifest;
using Deployment.Platform.Domain.Planning;

public sealed class App
{
    private readonly IManifestProvider _manifestProvider;
    private readonly IRepositoryChangeProvider _repositoryChangeProvider;
    private readonly IImpactAnalyzer _impactAnalyzer;
    private readonly IDeploymentPlanner _deploymentPlanner;
    public App(
        IManifestProvider manifestProvider,
        IRepositoryChangeProvider repositoryChangeProvider,
        IImpactAnalyzer impactAnalyzer,
        IDeploymentPlanner deploymentPlanner)
    {
        _manifestProvider = manifestProvider;
        _repositoryChangeProvider = repositoryChangeProvider;
        _impactAnalyzer = impactAnalyzer;
        _deploymentPlanner = deploymentPlanner;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("Deployment Platform");
        Console.WriteLine("-------------------");

        Console.WriteLine("Loading deployment manifest...");
        var manifest = await GetManifestAsync();

        // ConsolePrinter.PrintManifest(manifest);

        Console.WriteLine();

        Console.WriteLine("Checking for working directory changes...");
         var changeSet = await GetGitRepositoryChangesAsync();

        // ConsolePrinter.PrintGitRepositoryChanges(changeSet);

        Console.WriteLine();
        Console.WriteLine("Analyzing impact of changes on artifacts...");
        var impactResult = AnalyzeImpact(manifest, changeSet);
        // ConsolePrinter.PrintImpactAnalysisResult(impactResult);

        Console.WriteLine();
        Console.WriteLine("Generating deployment plan based on impacted artifacts...");
        DeploymentPlan impactedPlan = GenerateDeploymentPlan(manifest, DeploymentStrategy.Impacted, impactResult);
        ConsolePrinter.PrintDeploymentPlan(impactedPlan);

        Console.WriteLine();
        Console.WriteLine("Generating deployment plan based on selected artifacts...");
        List<string> selectedArtifacts = new List<string> { "claims-service", "gateway-service" };
        DeploymentPlan selectedPlan = GenerateDeploymentPlan(manifest, DeploymentStrategy.Selected, null, selectedArtifacts);
        ConsolePrinter.PrintDeploymentPlan(selectedPlan);

        Console.WriteLine();
        Console.WriteLine("Generating deployment plan for all artifacts...");
        DeploymentPlan fullPlan = GenerateDeploymentPlan(manifest, DeploymentStrategy.Full, null, null);
        ConsolePrinter.PrintDeploymentPlan(fullPlan);
    }

    private Task<RepositoryManifest> GetManifestAsync()
    {
        return _manifestProvider.LoadAsync();
    }

    private Task<ChangeSet> GetGitRepositoryChangesAsync()
    {
        return _repositoryChangeProvider.GetWorkingDirectoryChangesAsync();
    }

    private ImpactAnalysisResult AnalyzeImpact(RepositoryManifest manifest, ChangeSet changeSet)
    {

        return _impactAnalyzer.Analyze(manifest, changeSet);
    }

    private DeploymentPlan GenerateDeploymentPlan(
        RepositoryManifest manifest,
        DeploymentStrategy strategy,
        ImpactAnalysisResult? impact = null,
        IEnumerable<string>? selectedArtifacts = null)
    {
        var request = new DeploymentPlanRequest
        {
            Manifest = manifest,
            Strategy = strategy,
            ImpactAnalysis = impact ?? new(),
            SelectedArtifacts = selectedArtifacts?.ToList() ?? []
        };

        return _deploymentPlanner.CreatePlan(request);
    }

}