using Deployment.Platform.Application.Interfaces.Process;
using Deployment.Platform.Application.Models;
using System.Diagnostics;
using System.ComponentModel;

namespace Deployment.Platform.Infrastructure.Processes;

public sealed class ProcessRunner : IProcessRunner
{
    public Task<ProcessResult> ExecuteAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default) =>
        RunProcessAsync(fileName, arguments, workingDirectory, cancellationToken);

    public Task<ProcessResult> ExecuteShellCommandAsync(
        string command,
        CancellationToken cancellationToken) =>
        RunProcessAsync("cmd.exe", $"/c {command}", null, cancellationToken, command);

    private static async Task<ProcessResult> RunProcessAsync(
        string fileName,
        string arguments,
        string? workingDirectory,
        CancellationToken cancellationToken,
        string? displayCommand = null)
    {
        try
        {   
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start the process.");
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);

            return new ProcessResult
            {
                FileName = fileName,
                Arguments = displayCommand ?? arguments,
                StandardOutput = outputTask.Result,
                StandardError = errorTask.Result,
                ExitCode = process.ExitCode
            };
        }
        catch (Win32Exception ex)
        {
            return new ProcessResult
            {
                FileName = fileName,
                Arguments = displayCommand ?? arguments,
                ExitCode = -1,
                StandardError = ex.Message
            };
        }
    }
}
