using Deployment.Platform.Application.Models.Execution;

namespace Deployment.Platform.Application.Interfaces.Configuration;

public interface IDeploymentEnvironmentProvider
{
    Task<DeploymentEnvironment> GetAsync(
        string environmentName,
        string settingsPath,
        CancellationToken cancellationToken = default);
}