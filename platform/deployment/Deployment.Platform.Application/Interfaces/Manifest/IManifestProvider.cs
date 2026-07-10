namespace Deployment.Platform.Application.Interfaces;

using Deployment.Platform.Domain.Manifest;

public interface IManifestProvider
{
    Task<RepositoryManifest> LoadAsync(
        CancellationToken cancellationToken = default);
}