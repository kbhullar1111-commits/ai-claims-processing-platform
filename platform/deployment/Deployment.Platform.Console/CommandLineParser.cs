using Deployment.Platform.Domain.Planning;
public static class CommandLineParser
{
    public static DeploymentCommand Parse(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException("No command specified.");
        }

        if (!args[0].Equals("deploy", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Unknown command '{args[0]}'.");
        }

        DeploymentStrategy? strategy = null;
        string? environment = null;
        bool dryRun = false;
        bool autoApprove = false;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--strategy":
                    EnsureValue(args, i);
                    if(Enum.TryParse<DeploymentStrategy>(
                        args[++i],
                        ignoreCase: true,
                        out var parsedStrategy)
                    )
                        strategy = parsedStrategy;
                    break;

                case "--environment":
                    EnsureValue(args, i);
                    environment = args[++i];
                    break;

                case "--dry-run":
                    dryRun = true;
                    break;

                case "--auto-approve":
                    autoApprove = true;
                    break;

                default:
                    throw new ArgumentException(
                        $"Unknown argument '{args[i]}'.");
            }
        }

        if (strategy is null)
        {
            throw new ArgumentException(
                "--strategy is required.");
        }

        if (string.IsNullOrWhiteSpace(environment))
        {
            throw new ArgumentException(
                "--environment is required.");
        }

        return new DeploymentCommand
        {
            Strategy = strategy.Value,
            Environment = environment,
            DryRun = dryRun,
            AutoApprove = autoApprove,
            Target = DeploymentTarget.AzureContainerApps
        };
    }

    private static void EnsureValue(string[] args, int index)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException(
                $"Missing value for '{args[index]}'.");
        }
    }
}