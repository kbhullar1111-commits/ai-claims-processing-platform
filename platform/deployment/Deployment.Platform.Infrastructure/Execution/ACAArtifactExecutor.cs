using Deployment.Platform.Application.Interfaces.Execution;
using Deployment.Platform.Application.Models;
using Deployment.Platform.Application.Models.Execution;
using Deployment.Platform.Domain.Execution;
using Deployment.Platform.Infrastructure.Azure;
using Deployment.Platform.Infrastructure.Docker;


namespace Deployment.Platform.Infrastructure.Execution;

public sealed class ACAArtifactExecutor : IArtifactExecutor
{
    private readonly DockerClient _dockerClient;
    private readonly ACAClient _acaClient;

    public ACAArtifactExecutor(
        DockerClient dockerClient,
        ACAClient acaClient)
    {
        _dockerClient = dockerClient;
        _acaClient = acaClient;
    }
    public async Task<ArtifactExecutionResult> ExecuteAsync(
        ExecutionArtifact artifact,
        string imageTag,
        DeploymentEnvironment deploymentEnvironment,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifact.Artifact.Dockerfile);

        var startedAt = DateTime.UtcNow;

        var dockerBuildResult = await _dockerClient.BuildImageAsync(
            artifact.Artifact.Dockerfile,
            artifact.Artifact.Name,
            cancellationToken);

        var failureResult = GetFailureResult(dockerBuildResult, artifact.Artifact.Name, startedAt);
        if (failureResult is not null)
        {
            return failureResult;
        }

        var artifactName = artifact.Artifact.Name;
        var taggedImageName = $"{deploymentEnvironment.ContainerRegistryServer}/{artifactName}:{imageTag}";

        var dockerTagResult = await _dockerClient.TagImageAsync(artifactName, taggedImageName, cancellationToken);
        failureResult = GetFailureResult(dockerTagResult, artifactName, startedAt);
        if (failureResult is not null)
        {
            return failureResult;
        }

        var dockerPushResult = await _dockerClient.PushImageAsync(taggedImageName, cancellationToken);
        failureResult = GetFailureResult(dockerPushResult, artifactName, startedAt);
        if (failureResult is not null)
        {
            return failureResult;
        }

        var validateContainerAppExistResult = await _acaClient.ValidateArtifactTargetAsync(
            artifactName, deploymentEnvironment.ResourceGroup, cancellationToken);
        failureResult = GetFailureResult(validateContainerAppExistResult, artifactName, startedAt);
        if (failureResult is not null)
        {
            return failureResult;
        }

        var updateContainerAppResult = await _acaClient.UpdateContainerAppAsync(
            artifactName, deploymentEnvironment.ResourceGroup, taggedImageName, cancellationToken
        );

        return CreateArtifactExecutionResult(updateContainerAppResult,
            artifact.Artifact.Name, startedAt);

        ArtifactExecutionResult? GetFailureResult(ProcessResult processResult, string artifactName, DateTime started)
        {
            if (processResult.Successful)
            {
                return null;
            }

            return CreateArtifactExecutionResult(processResult, artifactName, started);
        }
    }

    private ArtifactExecutionResult CreateArtifactExecutionResult(
        ProcessResult processResult,
        string artifactName,
        DateTime startedAt)
    {
        var errorMessage = !processResult.Successful ? processResult.StandardError : null;

        return new ArtifactExecutionResult
        {
            ArtifactName = artifactName,

            StartedAt = startedAt,

            CompletedAt = DateTime.UtcNow,

            Successful = processResult.Successful,

            ErrorMessage = errorMessage
        };

    }

}