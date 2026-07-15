using Deployment.Platform.Application.Interfaces.Process;
using Deployment.Platform.Application.Interfaces.Validation;
using Deployment.Platform.Application.Models;
using Deployment.Platform.Application.Models.Execution;

namespace Deployment.Platform.Infrastructure.Validation;

public sealed class AzureEnvironmentValidator : IDeploymentTargetValidator
{
    private readonly IProcessRunner _processRunner;

    public AzureEnvironmentValidator(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public async Task ValidateAsync(
        DeploymentEnvironment deploymentEnvironment,
        CancellationToken cancellationToken)
    {
        await CheckAzureCliAsync(cancellationToken);
        await CheckAuthenticationAsync(cancellationToken);
        await CheckSubscriptionAsync(cancellationToken);
    }

    private async Task CheckAzureCliAsync(CancellationToken cancellationToken)
    {
        var result = await _processRunner.ExecuteShellCommandAsync("az --version", cancellationToken: cancellationToken);
        ValidateProcessResult(result, "Failed to execute Azure CLI.");
    }

    private async Task CheckAuthenticationAsync(CancellationToken cancellationToken)
    {
        var result = await _processRunner.ExecuteShellCommandAsync("az account show --output json", cancellationToken: cancellationToken);
        ValidateProcessResult(result, "Azure CLI authentication failed. Run 'az login' to authenticate.");
    }

    private async Task CheckSubscriptionAsync(CancellationToken cancellationToken)
    {
        var result = await _processRunner.ExecuteShellCommandAsync("az account show --query id --output tsv", cancellationToken: cancellationToken);
        ValidateProcessResult(result, "No Azure subscription is currently selected.");
    }

    private static void ValidateProcessResult(ProcessResult result, string errorMessage)
    {
        if (!result.Successful)
        {
            var details = !string.IsNullOrWhiteSpace(result.StandardError)
                ? result.StandardError.Trim()
                : result.StandardOutput.Trim();

            throw new InvalidOperationException(
                $"{errorMessage}{Environment.NewLine}{Environment.NewLine}" +
                $"Command:{Environment.NewLine}" +
                $"{result.FileName} {result.Arguments}{Environment.NewLine}{Environment.NewLine}" +
                $"Details:{Environment.NewLine}" +
                $"{details}");
        }

        if (string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            throw new InvalidOperationException(errorMessage);
        }
    }
}
