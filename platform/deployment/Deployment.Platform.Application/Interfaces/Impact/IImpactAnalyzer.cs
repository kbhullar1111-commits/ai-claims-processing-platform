using Deployment.Platform.Domain.Manifest;
using Deployment.Platform.Domain.Impact;
using Deployment.Platform.Domain.Changes;

namespace Deployment.Platform.Application.Interfaces.Impact;

public interface IImpactAnalyzer
{
    ImpactAnalysisResult Analyze(
        RepositoryManifest manifest,
        ChangeSet changeSet);
}