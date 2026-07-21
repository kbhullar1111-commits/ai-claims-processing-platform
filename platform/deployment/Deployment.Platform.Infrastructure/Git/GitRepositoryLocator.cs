using Deployment.Platform.Domain.Changes;
using Deployment.Platform.Application.Interfaces.Changes;
using Deployment.Platform.Application.Models;
using Deployment.Platform.Application.Interfaces.Process;

namespace Deployment.Platform.Infrastructure.Git;

public sealed class GitRepositoryLocator : IRepositoryLocator
{
    private readonly IProcessRunner _processRunner;

    private readonly SemaphoreSlim _lock = new(1,1);
    
    private string? _repositoryRoot;

    public GitRepositoryLocator(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public async Task<string> GetRepositoryRootAsync(
        CancellationToken cancellationToken = default)
    {
        if (_repositoryRoot is not null)
            return _repositoryRoot;

        await _lock.WaitAsync(cancellationToken);

        try
        {

            var processResult = await _processRunner.ExecuteAsync(
                "git",
                "rev-parse --show-toplevel",
                null,
                cancellationToken
            );

            if (!processResult.Successful)
            {
                throw new InvalidOperationException(
                    $"Git command failed with exit code {processResult.ExitCode}: {processResult.StandardError}");
            }

            _repositoryRoot = processResult.StandardOutput.Trim();

            ArgumentException.ThrowIfNullOrWhiteSpace(_repositoryRoot);

            if (!Directory.Exists(_repositoryRoot))
            {
                throw new DirectoryNotFoundException(
                    $"The specified repository path '{_repositoryRoot}' does not exist.");
            }

            return _repositoryRoot;
        }
        finally
        {
            _lock.Release();
        }
    }

      

}