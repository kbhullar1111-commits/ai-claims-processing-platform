using Deployment.Platform.Application.Models;
using Deployment.Platform.Application.Interfaces.Process;
using Deployment.Platform.Infrastructure.Utilities;

namespace Deployment.Platform.Infrastructure.Docker;

public sealed class DockerClient
{
    private readonly IProcessRunner _processRunner;
    private readonly string _repositoryPath;

    public DockerClient(        
        RepositoryOptions repositoryOptions,
        IProcessRunner processRunner)
    {
        ArgumentNullException.ThrowIfNull(repositoryOptions);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            repositoryOptions.RepositoryPath);

        _repositoryPath = FileSystemPathUtility.NormalizePath(repositoryOptions.RepositoryPath);

        if (!Directory.Exists(_repositoryPath))
        {
            throw new DirectoryNotFoundException(
                $"The specified repository path '{_repositoryPath}' does not exist.");
        }

        _processRunner = processRunner;
    }

    public async Task<ProcessResult> BuildImageAsync(
        string dockerfilePath,
        string imageName,
        CancellationToken cancellationToken)
    {
        var dockerfilePathNormalized = FileSystemPathUtility.NormalizePath(dockerfilePath);
        var command =$"build -f {dockerfilePathNormalized} -t {imageName} .";
        return await _processRunner.ExecuteAsync(
            "docker",
            command,
            _repositoryPath,
            cancellationToken
        );
    }

    public async Task<ProcessResult> TagImageAsync(
        string imageName,
        string acrServer,
        string imageTag,
        CancellationToken cancellationToken)
    {
        var command =$"tag {imageName} {acrServer}/{imageName}:{imageTag}";
        return await _processRunner.ExecuteAsync(
            "docker",
            command,
            _repositoryPath,
            cancellationToken
        );
    }
}