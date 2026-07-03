using Deployment.Platform.Application.Interfaces;
using Deployment.Platform.Application.Models;
using Deployment.Platform.Infrastructure.Manifest;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Deployment Platform");
        Console.WriteLine("-------------------");

        await PrintManifest();

        Console.WriteLine();

        await PrintGitRepositoryChanges();
    }

    static async Task PrintManifest()
    {
        var provider =
            new YamlManifestProvider(
                "../../../deployment.manifest.yaml");

        Console.WriteLine("Loading deployment manifest...");

        var manifest = await provider.LoadAsync();

        Console.WriteLine("Manifest loaded successfully.");

        Console.WriteLine($"Version: {manifest.Version}");

        Console.WriteLine($"Repository: {manifest.Repository?.Name ?? "N/A"}");

        foreach (var artifact in manifest.Artifacts)
        {
            Console.WriteLine();

            Console.WriteLine(artifact.Name);

            Console.WriteLine($"Type: {artifact.Type}");

            Console.WriteLine($"Project: {artifact.Project}");

            Console.WriteLine($"Dockerfile: {artifact.Dockerfile ?? "N/A"}");

            foreach (var dependency in artifact.Dependencies)
            {
                Console.WriteLine($"  -> {dependency}");
            }
        }
    }

    static async Task PrintGitRepositoryChanges()
    {
        var repositoryOptions = new RepositoryOptions
        {
            RepositoryPath = "../../../"
        };

        var gitChangeProvider =
            new Deployment.Platform.Infrastructure.Git.GitRepositoryChangeProvider(
                repositoryOptions);

        Console.WriteLine("Checking for working directory changes...");

        var changeSet = await gitChangeProvider.GetWorkingDirectoryChangesAsync();

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

}
