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
        DeploymentTarget? target = null;
        string? environment = null;
        bool dryRun = false;
        bool autoApprove = false;
        string? baseCommit = null;
        string? headCommit = null;
        string? manifestPath = null;
        string? settingsPath = null;
        string? selectedArtifacts = null;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--strategy":
                    EnsureValue(args, i);
                    if (Enum.TryParse<DeploymentStrategy>(
                        args[++i],
                        ignoreCase: true,
                        out var parsedStrategy)
                    )
                    {
                        strategy = parsedStrategy;
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"Unknown deployment strategy '{args[i]}'.");
                    }
                    break;

                case "--target":
                    EnsureValue(args, i);
                    if (Enum.TryParse<DeploymentTarget>(
                        args[++i],
                        ignoreCase: true,
                        out var parsedTarget)
                    )
                    {
                        target = parsedTarget;
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"Unknown deployment target '{args[i]}'.");
                    }
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

                case "--base":
                    EnsureValue(args, i);
                    baseCommit = args[++i];
                    break;

                case "--head":
                    EnsureValue(args, i);
                    headCommit = args[++i];
                    break;

                case "--manifest":
                    EnsureValue(args, i);
                    manifestPath = args[++i];
                    break;

                case "--settings":
                    EnsureValue(args, i);
                    settingsPath = args[++i];
                    break;

                case "--artifacts":
                    EnsureValue(args, i);
                    selectedArtifacts = args[++i];
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

        if(strategy == DeploymentStrategy.Selected && selectedArtifacts == null)
        {
            throw new ArgumentException(
                "--artifacts required for strategy type - selected.");
        }

        if (target is null)
        {
            throw new ArgumentException(
                "--target is required.");
        }

        if (string.IsNullOrWhiteSpace(environment))
        {
            throw new ArgumentException(
                "--environment is required.");
        }

        if (string.IsNullOrWhiteSpace(baseCommit) ^
            string.IsNullOrWhiteSpace(headCommit))
        {
            throw new ArgumentException(
                "--base and --head must be specified together.");
        }

        return new DeploymentCommand
        {
            Strategy = strategy.Value,
            Environment = environment,
            DryRun = dryRun,
            AutoApprove = autoApprove,
            Target = target.Value,
            BaseCommit = baseCommit,
            HeadCommit = headCommit,
            ManifestPath = manifestPath ?? "deployment.manifest.yaml",
            SettingsPath = settingsPath ?? "deployment.settings.json",
            SelectedArtifacts = selectedArtifacts is null
            ? Array.Empty<string>()
            : selectedArtifacts
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim()).ToArray()

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