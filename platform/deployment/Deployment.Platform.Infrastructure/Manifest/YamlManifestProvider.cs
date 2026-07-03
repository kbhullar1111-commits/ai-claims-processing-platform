namespace Deployment.Platform.Infrastructure.Manifest;

using Deployment.Platform.Application.Interfaces;
using Deployment.Platform.Domain.Manifest;
using YamlDotNet.Serialization;

public sealed class YamlManifestProvider : IManifestProvider
{
    private readonly string _manifestPath;

    public YamlManifestProvider(string manifestPath)
    {
        _manifestPath = manifestPath;
    }

    public async Task<RepositoryManifest> LoadAsync(
        CancellationToken cancellationToken = default)
    {

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
            .WithEnumNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var yaml = await File.ReadAllTextAsync(
            _manifestPath,
            cancellationToken);

        var manifest = deserializer.Deserialize<RepositoryManifest>(yaml);

        if (manifest is null)
        {
            throw new InvalidOperationException(
                "Unable to load repository manifest.");
        }

        return manifest;
    }

}