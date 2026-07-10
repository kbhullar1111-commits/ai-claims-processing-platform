using Deployment.Platform.Application.Interfaces;
using Deployment.Platform.Application.Utilities;
using Deployment.Platform.Domain.Manifest;
using Deployment.Platform.Domain.Impact;
using Deployment.Platform.Domain.Changes;

namespace Deployment.Platform.Application.Services;

public class ImpactAnalyzer : IImpactAnalyzer
{
    public ImpactAnalysisResult Analyze(
        RepositoryManifest manifest,
        ChangeSet changeSet)
    {
        var impactedArtifacts = new List<ImpactedArtifact>();

        foreach (var artifact in manifest.Artifacts)
        {
            var artifactRoot = PathNormalizer.NormalizePath(artifact.Root);
            var ignoredPaths = artifact.IgnoredPaths
                .Select(ignoredPath => PathNormalizer.NormalizePath(Path.Combine(artifact.Root, ignoredPath)))
                .ToList();

            var dependencyPaths = artifact.Dependencies
                .Select(PathNormalizer.NormalizePath)
                .ToList();

            var changedFileImpacts = changeSet.Files
                .Select(file =>
                {
                    var normalizedFilePath = PathNormalizer.NormalizePath(file.Path);

                    if (IsIgnored(normalizedFilePath, ignoredPaths))
                    {
                        return null;
                    }

                    var impactType = GetImpactType(normalizedFilePath, artifactRoot, dependencyPaths);
                    if (impactType is null)
                    {
                        return null;
                    }

                    return new ChangedFileImpact(
                        file,
                        impactType.Value);
                })
                .Where(fileImpact => fileImpact is not null)
                .Select(fileImpact => fileImpact!)
                .ToList();


            if (changedFileImpacts.Any())
            {
                var impactType = changedFileImpacts.Any(fileImpact => fileImpact.Impact == ImpactType.Direct)
                    ? ImpactType.Direct
                    : ImpactType.Dependency;

                var changedFiles = changedFileImpacts.Select(fileImpact => fileImpact.ChangedFile).ToList();

                impactedArtifacts.Add(new ImpactedArtifact
                {
                    Artifact = artifact,
                    ImpactType = impactType,
                    ChangedFiles = changedFiles
                });
            }
        }

        return new ImpactAnalysisResult
        {
            Artifacts = impactedArtifacts
        };
    }

    private static bool IsIgnored(string filePath, IReadOnlyCollection<string> ignoredPaths)
    {
        return ignoredPaths.Any(ignoredPath => filePath.StartsWith(ignoredPath, StringComparison.OrdinalIgnoreCase));
    }

    private static ImpactType? GetImpactType(string filePath, string artifactRoot, IReadOnlyCollection<string> dependencies)
    {
        if (filePath.StartsWith(artifactRoot, StringComparison.OrdinalIgnoreCase))
        {
            return ImpactType.Direct;
        }

        return dependencies.Any(dependencyPath => filePath.StartsWith(dependencyPath, StringComparison.OrdinalIgnoreCase))
            ? ImpactType.Dependency
            : null;
    }

    private sealed record ChangedFileImpact(ChangedFile ChangedFile, ImpactType Impact);

}