namespace Deployment.Platform.Domain.Changes;

public sealed record ChangeSet(
    IReadOnlyCollection<ChangedFile> Files);