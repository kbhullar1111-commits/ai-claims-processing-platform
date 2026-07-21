using Deployment.Platform.Domain.Changes;

namespace Deployment.Platform.Application.Interfaces.Changes;

public interface IRepositoryChangeProvider
{
    Task<ChangeSet> GetWorkingDirectoryChangesAsync(CancellationToken cancellationToken = default);

    Task<ChangeSet> GetCommitChangesAsync(
        string baseCommit,
        string headCommit,
        CancellationToken cancellationToken = default);
}