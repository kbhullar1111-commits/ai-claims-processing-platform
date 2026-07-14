namespace Deployment.Platform.Application.Models.Execution;

public sealed class DeploymentExecutionResult
{
    public required DateTime StartedAt { get; init;}
    public required DateTime CompletedAt { get; init;}
    public bool Successful { get; init;}
    public required IReadOnlyList<StageExecutionResult> StageResults { get; init;}
}

public sealed  class StageExecutionResult
{
    public required DateTime StartedAt { get; init;}
    public required DateTime CompletedAt { get; init;}
    public required int StageOrder { get; init;}
    public bool Successful { get; init;}
    public required IReadOnlyList<ArtifactExecutionResult> ArtifactResults { get; init;}
}

public sealed class ArtifactExecutionResult
{
    public required string ArtifactName { get; init;}
    public bool Successful { get; init;}
    public required DateTime StartedAt { get; init;}
    public required DateTime CompletedAt { get; init;}
    public string? ErrorMessage { get; init;}
}