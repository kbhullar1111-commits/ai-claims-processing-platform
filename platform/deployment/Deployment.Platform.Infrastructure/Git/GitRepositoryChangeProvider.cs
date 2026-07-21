using Deployment.Platform.Domain.Changes;
using Deployment.Platform.Application.Interfaces.Changes;
using Deployment.Platform.Application.Interfaces.Process;


namespace Deployment.Platform.Infrastructure.Git;

public sealed class GitRepositoryChangeProvider
    : IRepositoryChangeProvider
{
    private readonly IRepositoryLocator _repositoryLocator;

    private readonly IProcessRunner _processRunner;

    public GitRepositoryChangeProvider(
        IRepositoryLocator repositoryLocator,
        IProcessRunner processRunner)
    {
        _repositoryLocator = repositoryLocator;
        _processRunner = processRunner;
    }

    public async Task<ChangeSet> GetWorkingDirectoryChangesAsync(CancellationToken cancellationToken = default)
    {

        var changeSet = await ExecuteGitCommand(
        "status --porcelain",
        ParseWorkingDirectoryChanges,
        cancellationToken);
       
        return changeSet;
    }

    public async Task<ChangeSet> GetCommitChangesAsync(
        string baseCommit,
        string headCommit,
        CancellationToken cancellationToken = default)
    {
        var changeSet = await ExecuteGitCommand(
        $"diff --name-only {baseCommit} {headCommit}",
        ParseCommitChanges,
        cancellationToken);
       
        return changeSet;
    }

    private async Task<ChangeSet> ExecuteGitCommand(
        string command,
        Func<string, ChangeSet> parser,
        CancellationToken cancellationToken)
    {
        string repositoryPath = await _repositoryLocator.GetRepositoryRootAsync(cancellationToken);
        var processResult = await _processRunner.ExecuteAsync(
            "git",
            command,
            repositoryPath,
            cancellationToken
        );

        if (!processResult.Successful)
        {
            throw new InvalidOperationException(
                $"Git command failed with exit code {processResult.ExitCode}: {processResult.StandardError}");
        }

        var changeSet = parser(processResult.StandardOutput);

        return changeSet;
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

    private static ChangeSet ParseCommitChanges(string output)
    {
        var files = output
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(path => path.Trim())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => new ChangedFile(path))
            .ToList();

        return new ChangeSet(files);
    }

}