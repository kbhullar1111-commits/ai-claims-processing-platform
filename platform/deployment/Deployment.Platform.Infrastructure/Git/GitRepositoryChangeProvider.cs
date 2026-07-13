using System.Diagnostics;
using Deployment.Platform.Domain.Changes;
using Deployment.Platform.Application.Interfaces.Changes;
using Deployment.Platform.Application.Models;
using Deployment.Platform.Application.Interfaces.Process;


namespace Deployment.Platform.Infrastructure.Git;

public sealed class GitRepositoryChangeProvider
    : IRepositoryChangeProvider
{
    private readonly string _repositoryPath;
    private const string _workingDirectoryCommand = "status --porcelain";

    private readonly IProcessRunner _processRunner;

    public GitRepositoryChangeProvider(
        RepositoryOptions repositoryOptions,
        IProcessRunner processRunner)
    {
        ArgumentNullException.ThrowIfNull(repositoryOptions);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            repositoryOptions.RepositoryPath);

        _repositoryPath = repositoryOptions.RepositoryPath;

        if (!Directory.Exists(_repositoryPath))
        {
            throw new DirectoryNotFoundException(
                $"The specified repository path '{_repositoryPath}' does not exist.");
        }

        _processRunner = processRunner;
    }

    public async Task<ChangeSet> GetWorkingDirectoryChangesAsync(CancellationToken cancellationToken = default)
    {

        var processResult = await _processRunner.ExecuteAsync(
            "git",
            _workingDirectoryCommand,
            _repositoryPath,
            cancellationToken
        );

        if (!processResult.Successful)
        {
            throw new InvalidOperationException(
                $"Git command failed with exit code {processResult.ExitCode}: {processResult.StandardError}");
        }

        var changeSet = ParseWorkingDirectoryChanges(processResult.StandardOutput);

        return changeSet;
    }

    public async Task<ChangeSet> GetCommitChangesAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement git commit changes loading
        return new ChangeSet([]);
    }

    private static ChangeSet ParseWorkingDirectoryChanges(string output)
    {
        var changedFiles = new List<ChangedFile>();
        foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            // The first two characters indicate the status of the file
            //var status = line.Substring(0, 2).Trim();
            if (line.Length < 4)
            {
                continue;
            }
            var filePath = line[3..].Trim();

            changedFiles.Add(new ChangedFile(filePath));
        }

        return new ChangeSet(changedFiles);
    }

}