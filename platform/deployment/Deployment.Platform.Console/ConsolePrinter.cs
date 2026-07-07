using Deployment.Platform.Domain.Impact;
using Deployment.Platform.Domain.Manifest;
using Deployment.Platform.Domain.Planning;
using Deployment.Platform.Domain.Changes;
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
}