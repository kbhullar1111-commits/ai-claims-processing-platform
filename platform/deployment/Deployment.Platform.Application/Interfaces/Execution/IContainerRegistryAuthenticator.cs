using Deployment.Platform.Application.Models;

namespace Deployment.Platform.Application.Interfaces.Execution;

public interface IContainerRegistryAuthenticator
{
    Task AuthenticateContainerRegistryAsync(
        string registryName,
        string registryServer,
        CancellationToken cancellationToken);
}