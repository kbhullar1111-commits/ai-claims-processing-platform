using Deployment.Platform.Application.Models;
using Deployment.Platform.Application.Interfaces.Changes;
using Deployment.Platform.Application.Interfaces.Process;
using Deployment.Platform.Infrastructure.Utilities;

namespace Deployment.Platform.Infrastructure.Docker;

public sealed class DockerClient
{
    private readonly IProcessRunner _processRunner;
    private readonly IRepositoryLocator _repositoryLocator;

    public DockerClient(        
        IRepositoryLocator repositoryLocator,
        IProcessRunner processRunner)
    {
        _repositoryLocator = repositoryLocator;
        _processRunner = processRunner;
    }

    public async Task<ProcessResult> BuildImageAsync(
        string dockerfilePath,
        string imageName,
        CancellationToken cancellationToken)
    {
        var dockerfilePathNormalized = FileSystemPathUtility.NormalizePath(dockerfilePath);
        var command =$"build -f {dockerfilePathNormalized} -t {imageName} .";
        return await ExecuteDockerCommand(command, cancellationToken);
    }

    public async Task<ProcessResult> TagImageAsync(
        string imageName,
        string taggedImageName,
        CancellationToken cancellationToken)
    {
        var command =$"tag {imageName} {taggedImageName}";
        return await ExecuteDockerCommand(command, cancellationToken);
    }

    public async Task<ProcessResult> PushImageAsync(
        string taggedImageName,
        CancellationToken cancellationToken)
    {
        var command =$"push {taggedImageName}";
        return await ExecuteDockerCommand(command, cancellationToken);
    }

    private async Task<ProcessResult> ExecuteDockerCommand(
        string command,
        CancellationToken cancellationToken)
    {
        var repositoryRoot =
            await _repositoryLocator.GetRepositoryRootAsync(cancellationToken);

        return await _processRunner.ExecuteAsync(
            "docker",
            command,
            repositoryRoot,
            cancellationToken);
    }
}