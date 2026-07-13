using Deployment.Platform.Application.Interfaces.Manifest;
using Deployment.Platform.Application.Interfaces.Changes;
using Deployment.Platform.Application.Interfaces.Impact;
using Deployment.Platform.Application.Interfaces.Planning;
using Deployment.Platform.Application.Interfaces.Execution;
using Deployment.Platform.Application.Models.Planning;
using Deployment.Platform.Application.Models.Execution;
using Deployment.Platform.Domain.Changes;
using Deployment.Platform.Domain.Impact;
using Deployment.Platform.Domain.Manifest;
using Deployment.Platform.Domain.Planning;
using Deployment.Platform.Domain.Execution;

public sealed class App
{
    private readonly IManifestProvider _manifestProvider;
    private readonly IRepositoryChangeProvider _repositoryChangeProvider;
    private readonly IImpactAnalyzer _impactAnalyzer;
    private readonly IDeploymentPlanner _deploymentPlanner;
    private readonly IExecutionGraphBuilder _graphBuilder;
    private readonly IDeploymentExecutor _deploymentExecutor;

    public App(
        IManifestProvider manifestProvider,
        IRepositoryChangeProvider repositoryChangeProvider,
        IImpactAnalyzer impactAnalyzer,
        IDeploymentPlanner deploymentPlanner,
        IExecutionGraphBuilder graphBuilder,
        IDeploymentExecutor deploymentExecutor)
    {
        _manifestProvider = manifestProvider;
        _repositoryChangeProvider = repositoryChangeProvider;
        _impactAnalyzer = impactAnalyzer;
        _deploymentPlanner = deploymentPlanner;
        _graphBuilder = graphBuilder;
        _deploymentExecutor = deploymentExecutor;
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
        //ConsolePrinter.PrintDeploymentPlan(impactedPlan);
        Console.WriteLine();
        Console.WriteLine("Building execution graph for impacted deployment plan...");
        ExecutionGraph executionGraph = CreateExecutionGraph(impactedPlan, manifest);
        var deploymentExecutionRequest = new DeploymentExecutionRequest
        {
            Environment = "Dev",
            ExecutionGraph = executionGraph,
            DryRun = true,
            AutoApprove = false
        };
        var deploymentExecutionResult =  await _deploymentExecutor.ExecuteAsync(deploymentExecutionRequest);
        //ConsolePrinter.PrintExecutionGraph(executionGraph);
        Console.WriteLine("Executing the deployment plan...");
        ConsolePrinter.PrintDeploymentExecutionResult(deploymentExecutionResult);

        // Console.WriteLine();
        // Console.WriteLine("Generating deployment plan based on selected artifacts...");
        // List<string> selectedArtifacts = new List<string> { "claims-service", "gateway-service" };
        // DeploymentPlan selectedPlan = GenerateDeploymentPlan(manifest, DeploymentStrategy.Selected, null, selectedArtifacts);
        // //ConsolePrinter.PrintDeploymentPlan(selectedPlan);
        // Console.WriteLine();
        // Console.WriteLine("Building execution graph for selected deployment plan...");
        // ExecutionGraph selectedExecutionGraph = CreateExecutionGraph(selectedPlan, manifest);
        // ConsolePrinter.PrintExecutionGraph(selectedExecutionGraph);

        // Console.WriteLine();
        // Console.WriteLine("Generating deployment plan for all artifacts...");
        // DeploymentPlan fullPlan = GenerateDeploymentPlan(manifest, DeploymentStrategy.Full, null, null);
        // //ConsolePrinter.PrintDeploymentPlan(fullPlan);
        // Console.WriteLine();
        // Console.WriteLine("Building execution graph for full deployment plan...");
        // ExecutionGraph fullExecutionGraph = CreateExecutionGraph(fullPlan, manifest);
        // ConsolePrinter.PrintExecutionGraph(fullExecutionGraph);
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

    private ExecutionGraph CreateExecutionGraph(DeploymentPlan plan, RepositoryManifest manifest)
    {
        return _graphBuilder.Build(plan, manifest);
    }

}