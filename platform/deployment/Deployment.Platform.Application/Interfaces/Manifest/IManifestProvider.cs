using Deployment.Platform.Domain.Manifest;

namespace Deployment.Platform.Application.Interfaces.Manifest;

public interface IManifestProvider
{
    Task<RepositoryManifest> LoadAsync(
        CancellationToken cancellationToken = default);
}