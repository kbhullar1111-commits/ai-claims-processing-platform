using Deployment.Platform.Application.Interfaces.Changes;
using Deployment.Platform.Application.Interfaces.Manifest;
using Deployment.Platform.Domain.Manifest;
using Deployment.Platform.Infrastructure.Utilities;
using YamlDotNet.Serialization;

namespace Deployment.Platform.Infrastructure.Manifest;

public sealed class YamlManifestProvider : IManifestProvider
{
    private readonly string _manifestRelativePath;

    private readonly IRepositoryLocator _repositoryLocator;

    public YamlManifestProvider(string manifestRelativePath, IRepositoryLocator repositoryLocator)
    {
        ArgumentException.ThrowIfNullOrEmpty(manifestRelativePath);
        _manifestRelativePath = manifestRelativePath;
        _repositoryLocator = repositoryLocator;
    }

    public async Task<RepositoryManifest> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        string repositoryRoot = await _repositoryLocator.GetRepositoryRootAsync(cancellationToken);

        string manifestPath = FileSystemPathUtility.NormalizePath(Path.Combine(repositoryRoot,_manifestRelativePath));

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
            .WithEnumNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var yaml = await File.ReadAllTextAsync(
            manifestPath,
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