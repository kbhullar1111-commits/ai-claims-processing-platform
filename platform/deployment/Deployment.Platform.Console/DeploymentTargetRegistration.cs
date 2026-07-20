using Microsoft.Extensions.DependencyInjection;
using Deployment.Platform.Application.Interfaces.Execution;
using Deployment.Platform.Application.Interfaces.Validation;
using Deployment.Platform.Infrastructure.Execution;
using Deployment.Platform.Infrastructure.Azure;
using Deployment.Platform.Infrastructure.Validation;
public static class DeploymentTargetRegistration
{
    public static void RegisterDeploymentTarget(
        IServiceCollection services,
        DeploymentTarget target)
    {
        switch (target)
        {
            case DeploymentTarget.AzureContainerApps:

                services.AddSingleton<IArtifactExecutor,
                    ACAArtifactExecutor>();

                services.AddSingleton<IDeploymentTargetValidator,
                    AzureEnvironmentValidator>();

                services.AddSingleton<IContainerRegistryAuthenticator,
                    ACRAuthenticator>();

                services.AddSingleton<ACAClient>();

                break;

            default:
                throw new NotSupportedException(
                    $"Deployment target '{target}' is not supported.");
        }
    }
}