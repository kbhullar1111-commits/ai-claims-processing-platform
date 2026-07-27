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
using Deployment.Platform.Application.Interfaces.Configuration;

public sealed class App
{
    private readonly IManifestProvider _manifestProvider;
    private readonly IRepositoryChangeProvider _repositoryChangeProvider;
    private readonly IImpactAnalyzer _impactAnalyzer;
    private readonly IDeploymentPlanner _deploymentPlanner;
    private readonly IExecutionGraphBuilder _graphBuilder;
    private readonly IDeploymentExecutor _deploymentExecutor;
    private readonly IDeploymentEnvironmentProvider _deploymentEnvironmentProvider;
    private readonly IRepositoryLocator _repositoryLocator;

    public App(
        IManifestProvider manifestProvider,
        IDeploymentEnvironmentProvider deploymentEnvironmentProvider,
        IRepositoryChangeProvider repositoryChangeProvider,
        IImpactAnalyzer impactAnalyzer,
        IDeploymentPlanner deploymentPlanner,
        IExecutionGraphBuilder graphBuilder,
        IDeploymentExecutor deploymentExecutor,
        IRepositoryLocator repositoryLocator)
    {
        _manifestProvider = manifestProvider;
        _deploymentEnvironmentProvider = deploymentEnvironmentProvider;
        _repositoryChangeProvider = repositoryChangeProvider;
        _impactAnalyzer = impactAnalyzer;
        _deploymentPlanner = deploymentPlanner;
        _graphBuilder = graphBuilder;
        _deploymentExecutor = deploymentExecutor;
        _repositoryLocator = repositoryLocator;
    }

    public async Task RunAsync(DeploymentCommand command)
    {
        Console.WriteLine("Deployment Platform");
        Console.WriteLine("-------------------");

        Console.WriteLine("Loading deployment manifest...");
        var manifest = await GetManifestAsync();

        // ConsolePrinter.PrintManifest(manifest);

        Console.WriteLine();

        ImpactAnalysisResult? impactResult = null;
        if(command.Strategy == DeploymentStrategy.Impacted)
        {
            Console.WriteLine("Checking for working directory changes...");
            
            ChangeSet changeSet;

            if (!string.IsNullOrWhiteSpace(command.BaseCommit) && !string.IsNullOrWhiteSpace(command.HeadCommit))
            {
                changeSet = await _repositoryChangeProvider.GetCommitChangesAsync(command.BaseCommit, command.HeadCommit);
            }
            else
            {
                changeSet = await GetGitRepositoryChangesAsync();
            }

            // ConsolePrinter.PrintGitRepositoryChanges(changeSet);
            

            Console.WriteLine();

            Console.WriteLine("Analyzing impact of changes on artifacts...");
            impactResult = AnalyzeImpact(manifest, changeSet);
            if (impactResult.Artifacts.Count == 0)
            {
                Console.WriteLine("No impacted artifacts found. Nothing to deploy.");
                return;
            }
            // ConsolePrinter.PrintImpactAnalysisResult(impactResult);
        }

        Console.WriteLine();
        Console.WriteLine($"Generating deployment plan based on {command.Strategy} artifacts...");
        DeploymentPlan deploymentPlan = command.Strategy == DeploymentStrategy.Selected 
        ? GenerateDeploymentPlan(manifest, command.Strategy, impactResult, command.SelectedArtifacts)
        : GenerateDeploymentPlan(manifest, command.Strategy, impactResult);
        //ConsolePrinter.PrintDeploymentPlan(impactedPlan);
        Console.WriteLine();
        Console.WriteLine($"Building execution graph for {command.Strategy} deployment plan...");
        ExecutionGraph executionGraph = CreateExecutionGraph(deploymentPlan, manifest);
        //ConsolePrinter.PrintExecutionGraph(executionGraph);
        DateTime utcNow = DateTime.UtcNow;
        string releaseName = $"release-{utcNow:yyyyMMdd-HHmmss}";

        string repositoryRoot = await _repositoryLocator.GetRepositoryRootAsync();
        string settingsPath = Path.Combine(repositoryRoot, command.SettingsPath);
        var deploymentEnvironment = await _deploymentEnvironmentProvider.GetAsync(command.Environment, settingsPath);

        var deploymentExecutionRequest = new DeploymentExecutionRequest
        {
            ExecutionGraph = executionGraph,
            DryRun = command.DryRun,
            AutoApprove = command.AutoApprove,
            ImageTag = releaseName,
            DeploymentEnvironment = deploymentEnvironment
        };
        Console.WriteLine("Executing the deployment plan...");
        var deploymentExecutionResult =  await _deploymentExecutor.ExecuteAsync(deploymentExecutionRequest);
        ConsolePrinter.PrintDeploymentExecutionResult(deploymentExecutionResult);

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