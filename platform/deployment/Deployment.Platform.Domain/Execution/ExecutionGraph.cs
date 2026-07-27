
namespace Deployment.Platform.Domain.Execution;

public sealed class ExecutionGraph
{
    private ExecutionGraph(IReadOnlyCollection<ExecutionStage> stages)
    {
        Stages = stages;
    }
    public IReadOnlyCollection<ExecutionStage> Stages { get; }

    public static ExecutionGraph Create(IReadOnlyCollection<ExecutionStage> stages)
    {
        ArgumentNullException.ThrowIfNull(stages);

        if (stages.Count == 0)
            throw new ArgumentException("Stages collection cannot be empty.", nameof(stages));

        return new ExecutionGraph(stages.ToList().AsReadOnly());
    }
}
