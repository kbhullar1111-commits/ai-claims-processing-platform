extern alias azureidentity;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using Serilog;
using Serilog.Events;
using Serilog.Enrichers.Span;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

var builder = FunctionsApplication.CreateBuilder(args);

var keyVaultEndpoint = builder.Configuration["KeyVault:Url"];

if (!string.IsNullOrEmpty(keyVaultEndpoint))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultEndpoint),
        new azureidentity::Azure.Identity.DefaultAzureCredential());
}

builder.ConfigureFunctionsWebApplication();

builder.Services.AddSerilog((services, loggerConfiguration) =>
{
    loggerConfiguration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithSpan()
        .Enrich.WithProperty("Service", "document-processor-function")
        .Enrich.WithProperty("Application", "BlobCreatedFunction")
        .WriteTo.Console()
        .WriteTo.ApplicationInsights(
            services.GetRequiredService<Microsoft.ApplicationInsights.TelemetryClient>(),
            TelemetryConverter.Traces);
});

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

var serviceBusNamespace = builder.Configuration["ServiceBus:FullyQualifiedNamespace"];

if (string.IsNullOrWhiteSpace(serviceBusNamespace))
{
    throw new InvalidOperationException(
        "Missing Service Bus configuration. Set ServiceBus:FullyQualifiedNamespace in configuration (Key Vault or environment variables).");
}

var clientOptions = new ServiceBusClientOptions
{
    TransportType = ServiceBusTransportType.AmqpWebSockets,
    RetryOptions = new ServiceBusRetryOptions
    {
        Mode = ServiceBusRetryMode.Exponential,
        MaxRetries = 5,
        Delay = TimeSpan.FromMilliseconds(800),
        MaxDelay = TimeSpan.FromSeconds(8),
        TryTimeout = TimeSpan.FromSeconds(60)
    }
};

builder.Services.AddSingleton(sp =>
{
    var credential = new azureidentity::Azure.Identity.DefaultAzureCredential();

    try
    {
        return new ServiceBusClient(serviceBusNamespace!, credential, clientOptions);
    }
    catch (Exception ex)
    {

        throw new InvalidOperationException(
            "Unable to create the Service Bus client using Managed Identity. Check that the managed identity has access to the Service Bus namespace and that the DefaultAzureCredential configuration is correct.",
            ex);
    }
});

builder.Build().Run();
