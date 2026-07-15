using Deployment.Platform.Application.Interfaces.Process;
using Deployment.Platform.Application.Interfaces.Validation;
using Deployment.Platform.Application.Models;

namespace Deployment.Platform.Infrastructure.Validation;

public sealed class ExecutionEnvironmentValidator : IExecutionEnvironmentValidator
{
    private readonly IProcessRunner _processRunner;

    public ExecutionEnvironmentValidator(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        await CheckGitCliAsync(cancellationToken);
        await CheckDockerRunning(cancellationToken);
    }

    private async Task CheckGitCliAsync(CancellationToken cancellationToken)
    {
        var result = await _processRunner.ExecuteAsync("git", "--version", cancellationToken: cancellationToken);
        ValidateProcessResult(result, "Git is not installed or not available in the PATH.");
    }

    private async Task CheckDockerRunning(CancellationToken cancellationToken)
    {
        var result = await _processRunner.ExecuteAsync("docker", "--version", cancellationToken: cancellationToken);
        ValidateProcessResult(result, "Docker daemon is not running or not accessible.");
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
