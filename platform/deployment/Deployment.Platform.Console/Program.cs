using Deployment.Platform.Application.Interfaces;
using Deployment.Platform.Application.Models;
using Deployment.Platform.Infrastructure.Manifest;
using Deployment.Platform.Application.Services;
using Deployment.Platform.Domain.Manifest;
using Deployment.Platform.Domain.Impact;
using Deployment.Platform.Domain.Changes;

class Program
{
    private const string manifestPath = "../../../deployment.manifest.yaml";
    static async Task Main(string[] args)
    {
        Console.WriteLine("Deployment Platform");
        Console.WriteLine("-------------------");

        await PrintManifest();

        Console.WriteLine();

        await PrintGitRepositoryChanges();

        Console.WriteLine();

        await PrintImpactAnalysisResult();

    }

    static async Task PrintManifest()
    {
        Console.WriteLine("Loading deployment manifest...");

        var manifest = await GetManifestAsync();

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

    static async Task PrintGitRepositoryChanges()
    {
        
        Console.WriteLine("Checking for working directory changes...");

        var changeSet = await GetGitRepositoryChangesAsync();

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

    static async Task PrintImpactAnalysisResult()
    {
        var manifest = await GetManifestAsync();
        var changeSet = await GetGitRepositoryChangesAsync();

        var impactAnalyzer = new ImpactAnalyzer();
        var impactResult = impactAnalyzer.Analyze(manifest, changeSet);

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

    static async Task<RepositoryManifest> GetManifestAsync()
    {
        var provider = new YamlManifestProvider(manifestPath);
        return await provider.LoadAsync();
    }

    static async Task<ChangeSet> GetGitRepositoryChangesAsync()
    {
        var repositoryOptions = new RepositoryOptions
        {
            RepositoryPath = "../../../"
        };

        var gitChangeProvider =
            new Deployment.Platform.Infrastructure.Git.GitRepositoryChangeProvider(
                repositoryOptions);

        return await gitChangeProvider.GetWorkingDirectoryChangesAsync();
    }

}
