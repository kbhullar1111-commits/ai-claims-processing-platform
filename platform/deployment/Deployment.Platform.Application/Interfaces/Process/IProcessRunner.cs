using Deployment.Platform.Application.Models;

namespace Deployment.Platform.Application.Interfaces.Process;

public interface IProcessRunner
{
    Task<ProcessResult> ExecuteAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default);
}