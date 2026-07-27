using Deployment.Platform.Domain.Manifest;
using Deployment.Platform.Domain.Changes;
using Deployment.Platform.Domain.Impact;
using Deployment.Platform.Domain.Planning;
using Deployment.Platform.Domain.Execution;
using Deployment.Platform.Application.Models.Execution;
static class ConsolePrinter
{
    internal static void PrintManifest(RepositoryManifest manifest)
    {

        Console.WriteLine("Manifest loaded successfully.");
        Console.WriteLine($"Version: {manifest.Version}");
        Console.WriteLine($"Repository: {manifest.Repository?.Name ?? "N/A"}");

        foreach (var artifact in manifest.Artifacts)
        {
            Console.WriteLine();
            Console.WriteLine(artifact.Name);
            Console.WriteLine($"Type: {artifact.Type}");
            Console.WriteLine($"Root: {artifact.Root}");
            Console.WriteLine($"Entry Point: {artifact.EntryPoint}");
            Console.WriteLine($"Dockerfile: {artifact.Dockerfile ?? "N/A"}");

            foreach (var dependency in artifact.Dependencies)
            {
                Console.WriteLine($"  -> {dependency}");
            }
        }
    }

    internal static void PrintGitRepositoryChanges(ChangeSet changeSet)
    {
        if (changeSet.Files.Count == 0)
        {
            Console.WriteLine("No changes detected.");
        }
        else
        {
            Console.WriteLine("Changes detected:");

            foreach (var change in changeSet.Files)
            {
                Console.WriteLine(change.Path);
            }
        }
    }

    internal static void PrintImpactAnalysisResult(ImpactAnalysisResult impactResult)
    {
        Console.WriteLine("Impact Analysis Result:");

        foreach (var artifact in impactResult.Artifacts)
        {
            Console.WriteLine($"Artifact: {artifact.Artifact.Name}");
            Console.WriteLine($"Impact Type: {artifact.ImpactType}");
            Console.WriteLine("Changed Files:");

            foreach (var changedFile in artifact.ChangedFiles)
            {
                Console.WriteLine($"  - {changedFile.Path}");
            }

            Console.WriteLine();
        }
    }

    internal static void PrintDeploymentPlan(DeploymentPlan plan)
    {
        Console.WriteLine("Deployment Plan:");
        Console.WriteLine($"Strategy: {plan.Strategy}");

        foreach (var artifact in plan.Artifacts)
        {
            Console.WriteLine($"Artifact: {artifact.Artifact.Name}");
            Console.WriteLine($"Type: {artifact.Artifact.Type}");
            Console.WriteLine($"Root: {artifact.Artifact.Root}");
            Console.WriteLine($"Entry Point: {artifact.Artifact.EntryPoint}");
            Console.WriteLine($"Dockerfile: {artifact.Artifact.Dockerfile ?? "N/A"}");
            Console.WriteLine();
        }
    }

    internal static void PrintExecutionGraph(ExecutionGraph executionGraph)
    {
        Console.WriteLine("Execution Graph:");

        foreach (var stage in executionGraph.Stages)
        {
            Console.WriteLine($"Stage Order: {stage.Order}");

            foreach (var artifact in stage.Artifacts)
            {
                Console.WriteLine($"  Artifact: {artifact.Artifact.Name}");
                Console.WriteLine($"  Type: {artifact.Artifact.Type}");
                Console.WriteLine($"  Root: {artifact.Artifact.Root}");
                Console.WriteLine($"  Entry Point: {artifact.Artifact.EntryPoint}");
                Console.WriteLine($"  Dockerfile: {artifact.Artifact.Dockerfile ?? "N/A"}");
                Console.WriteLine();
            }
        }
    }

    internal static void PrintDeploymentExecutionResult(DeploymentExecutionResult executionResult)
    {
        var status = executionResult.Successful ? "SUCCESS" : "FAILED";

        Console.WriteLine($"Deployment Execution Result: {status}");
        Console.WriteLine($"Started At: {executionResult.StartedAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Completed At: {executionResult.CompletedAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();

        foreach (var stageResult in executionResult.StageResults)
        {
            var stageStatus = stageResult.Successful ? "SUCCESS" : "FAILED";

            Console.WriteLine($"Stage {stageResult.StageOrder}: {stageStatus}");
            Console.WriteLine($"Stage Started At: {stageResult.StartedAt:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Stage Completed At: {stageResult.CompletedAt:yyyy-MM-dd HH:mm:ss}");

            foreach (var artifactResult in stageResult.ArtifactResults)
           {
                var artifactStatus = artifactResult.Successful ? "✓" : "✗";
                Console.WriteLine($"    {artifactStatus} {artifactResult.ArtifactName}");
                Console.WriteLine($"Artifact Started At: {artifactResult.StartedAt:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"Artifact Completed At: {artifactResult.CompletedAt:yyyy-MM-dd HH:mm:ss}");

                if (!string.IsNullOrEmpty(artifactResult.ErrorMessage))
                {
                    Console.WriteLine($"      Error: {artifactResult.ErrorMessage}");
                }
            }

            Console.WriteLine();
        }
    }

}