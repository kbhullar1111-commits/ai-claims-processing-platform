
namespace Deployment.Platform.Domain.Execution;

public sealed class ExecutionStage
{
    private ExecutionStage(int order, IReadOnlyCollection<ExecutionArtifact> artifacts)
    {
        Order = order;
        Artifacts = artifacts;
    }

    public int Order { get; }

    public IReadOnlyCollection<ExecutionArtifact> Artifacts { get; }


    public static ExecutionStage Create(
        int order,
        IReadOnlyCollection<ExecutionArtifact> artifacts)
    {
        ArgumentNullException.ThrowIfNull(artifacts);

        if (order < 1)
            throw new ArgumentOutOfRangeException(nameof(order));

        if (artifacts.Count == 0)
            throw new ArgumentException("Artifacts collection cannot be empty.", nameof(artifacts));

        return new ExecutionStage(order, artifacts.ToList().AsReadOnly());
    }

}