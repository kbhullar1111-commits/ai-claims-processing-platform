namespace Deployment.Platform.Application.Models;

public sealed class ProcessResult
{
    public int ExitCode { get; init; }

    public required string FileName { get; init; }

    public required string Arguments { get; init; }

    public string StandardOutput { get; init; } = string.Empty;

    public string StandardError { get; init; } = string.Empty;

    public bool Successful => ExitCode == 0;
}