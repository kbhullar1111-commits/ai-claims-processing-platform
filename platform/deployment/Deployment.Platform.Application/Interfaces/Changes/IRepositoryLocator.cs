using Deployment.Platform.Domain.Changes;

namespace Deployment.Platform.Application.Interfaces.Changes;

public interface IRepositoryLocator
{
    Task<string> GetRepositoryRootAsync(
        CancellationToken cancellationToken = default);
}