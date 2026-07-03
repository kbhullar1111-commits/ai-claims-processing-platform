namespace Deployment.Platform.Domain.Changes;

public sealed record ChangeSet(
    List<ChangedFile> Files);