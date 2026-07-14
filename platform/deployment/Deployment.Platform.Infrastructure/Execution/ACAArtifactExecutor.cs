using Deployment.Platform.Application.Interfaces.Execution;
using Deployment.Platform.Application.Models;
using Deployment.Platform.Application.Models.Execution;
using Deployment.Platform.Domain.Execution;
using Deployment.Platform.Infrastructure.Docker;


namespace Deployment.Platform.Infrastructure.Execution;

public sealed class ACAArtifactExecutor : IArtifactExecutor
{
    private readonly DockerClient _dockerClient;

    public ACAArtifactExecutor(
        DockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }
    public async Task<ArtifactExecutionResult> ExecuteAsync(
        ExecutionArtifact artifact,
        DeploymentEnvironment deploymentEnvironment,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifact.Artifact.Dockerfile);

        var startedAt = DateTime.UtcNow;
        var completedAt = DateTime.UtcNow;

        var dockerBuildResult = await _dockerClient.BuildImageAsync(
        artifact.Artifact.Dockerfile,
        artifact.Artifact.Name, 
        cancellationToken);

        if (!dockerBuildResult.Successful)
        {
            completedAt = DateTime.UtcNow;
            return CreateArtifactExecutionResult(dockerBuildResult, 
            artifact.Artifact.Name, startedAt, completedAt);
        }

        var dockerTagResult = await _dockerClient.TagImageAsync(
        artifact.Artifact.Name, 
        deploymentEnvironment.AcrServer,
        deploymentEnvironment.ImageTag,
        cancellationToken);

        completedAt = DateTime.UtcNow;

        return CreateArtifactExecutionResult(dockerTagResult,
        artifact.Artifact.Name, startedAt, completedAt);

    }

    private ArtifactExecutionResult CreateArtifactExecutionResult(
        ProcessResult processResult,
        string artifactName,
        DateTime startedAt,
        DateTime completedAt)
    {
        var errorMessage = !processResult.Successful ? processResult.StandardError : null;

        return new ArtifactExecutionResult
        {
            ArtifactName = artifactName,

            StartedAt = startedAt,

            CompletedAt = completedAt,

            Successful = processResult.Successful,

            ErrorMessage = errorMessage
        };

    }

}