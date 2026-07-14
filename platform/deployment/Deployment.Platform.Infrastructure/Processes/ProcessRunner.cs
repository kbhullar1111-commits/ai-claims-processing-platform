using Deployment.Platform.Application.Interfaces.Process;
using Deployment.Platform.Application.Models;
using System.Diagnostics;

namespace Deployment.Platform.Infrastructure.Processes;

public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> ExecuteAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,

            WorkingDirectory = workingDirectory,

            RedirectStandardOutput = true,
            RedirectStandardError = true,

            UseShellExecute = false,

            CreateNoWindow = true
        };

        using var process = Process.Start(processStartInfo);

        if (process is null)
        {
            throw new InvalidOperationException("Failed to start the process.");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync();

        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(cancellationToken);

        await Task.WhenAll(outputTask, errorTask);

        var processResult = new ProcessResult
        {
            FileName = fileName,
            Arguments = arguments,
            StandardOutput = outputTask.Result,
            StandardError = errorTask.Result,
            ExitCode = process.ExitCode
        };

        return processResult;

    }
}
