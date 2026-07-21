using System.Text.Json;
using System.Text.Json.Serialization;
using Deployment.Platform.Application.Interfaces.Configuration;
using Deployment.Platform.Application.Models;
using Deployment.Platform.Application.Models.Execution;
using Deployment.Platform.Infrastructure.Utilities;

namespace Deployment.Platform.Infrastructure.Configuration;

public sealed class JsonDeploymentEnvironmentProvider : IDeploymentEnvironmentProvider
{

    private static readonly JsonSerializerOptions JsonOptions =
    new()
    {
        PropertyNameCaseInsensitive = true
    };


    public async Task<DeploymentEnvironment> GetAsync(
        string environmentName,
        string settingsPath,
        CancellationToken cancellationToken = default)
    {
        var settingsPathNormalized = FileSystemPathUtility.NormalizePath(settingsPath);

        if (!File.Exists(settingsPathNormalized))
        {
            throw new FileNotFoundException(
                $"Deployment settings file '{settingsPathNormalized}' was not found.");
        }

        await using var stream = File.OpenRead(settingsPathNormalized);

        var settings = await JsonSerializer.DeserializeAsync<DeploymentSettingsFile>(
            stream,
            JsonOptions,
            cancellationToken);

        if (settings?.Environments is null)
        {
            throw new InvalidOperationException("Deployment settings file does not contain any environments.");
        }

        if (!settings.Environments.TryGetValue(environmentName, out var environmentSettings))
        {
            throw new KeyNotFoundException($"Environment '{environmentName}' was not found in deployment settings.");
        }

        return new DeploymentEnvironment
        {
            Name = environmentName,
            ResourceGroup = environmentSettings.ResourceGroup ?? throw new InvalidOperationException(
            $"Environment {environmentName} is missing 'resourceGroup'."),
            ContainerRegistryName = environmentSettings.ContainerRegistryName ?? throw new InvalidOperationException(
            $"Environment {environmentName} is missing 'containerRegistryName'."),
            ContainerRegistryServer = environmentSettings.ContainerRegistryServer ?? throw new InvalidOperationException(
            $"Environment {environmentName} is missing 'containerRegistryServer'."),
            ContainerAppEnvironment = environmentSettings.ContainerAppEnvironment ?? throw new InvalidOperationException(
            $"Environment {environmentName} is missing 'containerAppEnvironment'."),
        };
    }

    private sealed class DeploymentSettingsFile
    {
        [JsonPropertyName("environments")]
        public Dictionary<string, DeploymentSettingsEnvironment> Environments { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class DeploymentSettingsEnvironment
    {

        [JsonPropertyName("resourceGroup")]
        public string? ResourceGroup { get; init; }

        [JsonPropertyName("containerRegistryName")]
        public string? ContainerRegistryName { get; init; }

        [JsonPropertyName("containerRegistryServer")]
        public string? ContainerRegistryServer { get; init; }

        [JsonPropertyName("containerAppEnvironment")]
        public string? ContainerAppEnvironment { get; init; }
    }
}