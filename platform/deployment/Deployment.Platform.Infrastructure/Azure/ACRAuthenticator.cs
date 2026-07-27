using Deployment.Platform.Application.Models;
using Deployment.Platform.Application.Interfaces.Process;
using Deployment.Platform.Application.Interfaces.Execution;

namespace Deployment.Platform.Infrastructure.Azure;

public sealed class ACRAuthenticator : IContainerRegistryAuthenticator
{
    private readonly IProcessRunner _processRunner;
    public ACRAuthenticator(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public async Task AuthenticateContainerRegistryAsync(
        string registryName,
        string registryServer,
        CancellationToken cancellationToken)
    {
        var command =$"az acr login --name {registryName}";
        var registryLoginResult =  await _processRunner.ExecuteShellCommandAsync(command, cancellationToken);
        if (!registryLoginResult.Successful)
        {
            var details = !string.IsNullOrWhiteSpace(registryLoginResult.StandardError)
            ? registryLoginResult.StandardError.Trim()
            : registryLoginResult.StandardOutput.Trim();

            string errorMessage = "ACR login failed.";

            throw new InvalidOperationException(
            $"{errorMessage}{Environment.NewLine}{Environment.NewLine}" +
            $"Command:{Environment.NewLine}" +
            $"{registryLoginResult.FileName} {registryLoginResult.Arguments}{Environment.NewLine}{Environment.NewLine}" +
            $"Details:{Environment.NewLine}" +
            $"{details}");
        }
    }

}