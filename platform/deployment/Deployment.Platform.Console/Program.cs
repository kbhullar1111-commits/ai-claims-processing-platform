using Deployment.Platform.Application.Interfaces;
using Deployment.Platform.Application.Services;
using Deployment.Platform.Application.Models;
using Deployment.Platform.Infrastructure.Git;
using Deployment.Platform.Infrastructure.Manifest;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

const string manifestPath = "../../../deployment.manifest.yaml";

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton(new RepositoryOptions
        {
            RepositoryPath = "../../../"
        });

        services.AddSingleton<IManifestProvider>(_ =>
            new YamlManifestProvider(manifestPath));

        services.AddSingleton<IRepositoryChangeProvider, GitRepositoryChangeProvider>();

        services.AddSingleton<IImpactAnalyzer, ImpactAnalyzer>();

        services.AddSingleton<IDeploymentPlanner, DeploymentPlanner>();

        services.AddSingleton<IExecutionGraphBuilder, ExecutionGraphBuilder>();

        services.AddSingleton<App>();
    })
    .Build();

await host.Services
    .GetRequiredService<App>()
    .RunAsync();