using Deployment.Platform.Application.Interfaces;
using Deployment.Platform.Infrastructure.Manifest;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Deployment Platform");
        Console.WriteLine("-------------------");

        var provider =
            new YamlManifestProvider(
                "../../../deployment.manifest.yaml");

        Console.WriteLine("Loading manifest...");

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
}
