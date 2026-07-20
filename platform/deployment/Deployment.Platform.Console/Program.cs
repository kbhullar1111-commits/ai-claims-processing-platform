using Deployment.Platform.Application.Interfaces.Manifest;
using Deployment.Platform.Application.Interfaces.Changes;
using Deployment.Platform.Application.Interfaces.Impact;
using Deployment.Platform.Application.Interfaces.Planning;
using Deployment.Platform.Application.Interfaces.Execution;
using Deployment.Platform.Application.Interfaces.Process;
using Deployment.Platform.Application.Interfaces.Validation;
using Deployment.Platform.Application.Interfaces.Configuration;
using Deployment.Platform.Application.Services.Impact;
using Deployment.Platform.Application.Services.Planning;
using Deployment.Platform.Application.Services.Execution;
using Deployment.Platform.Application.Models;
using Deployment.Platform.Infrastructure.Git;
using Deployment.Platform.Infrastructure.Manifest;
using Deployment.Platform.Infrastructure.Processes;
using Deployment.Platform.Infrastructure.Docker;
using Deployment.Platform.Infrastructure.Validation;
using Deployment.Platform.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YamlDotNet.Core;

const string manifestPath = "../../../deployment.manifest.yaml";

DeploymentCommand command =
    CommandLineParser.Parse(args);

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices(services =>
    {
        services.AddSingleton(new RepositoryOptions
        {
            RepositoryPath = "../../../"
        });

        services.AddSingleton<IDeploymentEnvironmentProvider, JsonDeploymentEnvironmentProvider>();

        services.AddSingleton<IManifestProvider>(_ =>
            new YamlManifestProvider(manifestPath));

        services.AddSingleton<IRepositoryChangeProvider, GitRepositoryChangeProvider>();

        services.AddSingleton<IImpactAnalyzer, ImpactAnalyzer>();

        services.AddSingleton<IDeploymentPlanner, DeploymentPlanner>();

        services.AddSingleton<IExecutionGraphBuilder, ExecutionGraphBuilder>();

        //services.AddSingleton<IArtifactExecutor, ACAArtifactExecutor>(); 

        services.AddSingleton<IStageExecutor, StageExecutor>();

        services.AddSingleton<IDeploymentExecutor, DeploymentExecutor>();

        services.AddSingleton<IProcessRunner, ProcessRunner>();

        services.AddSingleton<IExecutionEnvironmentValidator, ExecutionEnvironmentValidator>();

        //services.AddSingleton<IDeploymentTargetValidator, AzureEnvironmentValidator>();

        //services.AddSingleton<IContainerRegistryAuthenticator, ACRAuthenticator>();

        services.AddSingleton<DockerClient>();

        services.AddSingleton<App>();

        DeploymentTargetRegistration.RegisterDeploymentTarget(services, command.Target);

    });

var host = builder.Build();


await host.Services
    .GetRequiredService<App>()
    .RunAsync(command);