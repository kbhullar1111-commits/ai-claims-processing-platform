using System.Diagnostics;
using Deployment.Platform.Domain.Changes;
using Deployment.Platform.Application.Interfaces;
using Deployment.Platform.Application.Models;


namespace Deployment.Platform.Infrastructure.Git;

public sealed class GitRepositoryChangeProvider
    : IRepositoryChangeProvider
{
    private readonly string _repositoryPath;
    private const string _workingDirectoryCommand = "status --porcelain";

    public GitRepositoryChangeProvider(
        RepositoryOptions repositoryOptions)
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
    }

    public async Task<ChangeSet> GetWorkingDirectoryChangesAsync(CancellationToken cancellationToken = default)
    {

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = _workingDirectoryCommand,

            WorkingDirectory = _repositoryPath,

            RedirectStandardOutput = true,
            RedirectStandardError = true,

            UseShellExecute = false,

            CreateNoWindow = true
        };

        using var process = Process.Start(processStartInfo);

        if (process is null)
        {
            throw new InvalidOperationException("Failed to start git process.");
        }

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Git command failed with exit code {process.ExitCode}: {error}");
        }

        var changeSet = ParseWorkingDirectoryChanges(output);

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