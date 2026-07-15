using Deployment.Platform.Application.Models;
using Deployment.Platform.Application.Interfaces.Process;

namespace Deployment.Platform.Infrastructure.Azure;

public sealed class ACAClient
{
    private readonly IProcessRunner _processRunner;
    public ACAClient(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public async Task<ProcessResult> AuthenticateRegistryAsync(
        string acrName,
        CancellationToken cancellationToken)
    {
        var command =$"az acr login --name {acrName}";
        return await _processRunner.ExecuteShellCommandAsync(command, cancellationToken);
    }

    public async Task<ProcessResult> UpdateContainerAppAsync(
        string artifactName,
        string  resourceGroup,
        string taggedImageName,
        CancellationToken cancellationToken)
    {
        var command = $"az containerapp update --name {artifactName} --resource-group {resourceGroup} --image {taggedImageName}";
        return await _processRunner.ExecuteShellCommandAsync(command, cancellationToken);
    }

    public async Task<ProcessResult> ValidateArtifactTargetAsync(
        string artifactName,
        string resourceGroup,
        CancellationToken cancellationToken)
    {
        var command = $"az containerapp show --name {artifactName} --resource-group {resourceGroup}";
        return await _processRunner.ExecuteShellCommandAsync(command, cancellationToken);
    }

    public async Task<ProcessResult> RestartAsync(
        string artifactName,
        string resourceGroup,
        CancellationToken cancellationToken)
    {
        var command = $"az containerapp restart --name {artifactName} --resource-group {resourceGroup}";
        return await _processRunner.ExecuteShellCommandAsync(command, cancellationToken);
    }

    public async Task<ProcessResult> GetRevisionAsync(
        string artifactName,
        string resourceGroup,
        string revisionName,
        CancellationToken cancellationToken)
    {
        var command = $"az containerapp revision show --name {artifactName} --resource-group {resourceGroup} --revision {revisionName}";
        return await _processRunner.ExecuteShellCommandAsync(command, cancellationToken);
    }

    public async Task<ProcessResult> GetLogsAsync(
        string artifactName,
        string resourceGroup,
        string revisionName,
        CancellationToken cancellationToken)
    {
        var command = $"az containerapp logs show --name {artifactName} --resource-group {resourceGroup} --revision {revisionName}";
        return await _processRunner.ExecuteShellCommandAsync(command, cancellationToken);
    }

}