extern alias azureidentity;

using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Enrichers.Span;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

var builder = WebApplication.CreateBuilder(args);

var keyVaultEndpoint = builder.Configuration["KeyVault:Url"];

if (!string.IsNullOrEmpty(keyVaultEndpoint))
{
    try
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultEndpoint),
            new azureidentity::Azure.Identity.DefaultAzureCredential());

        builder.Configuration.AddEnvironmentVariables();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Key Vault is unavailable. Continuing with local configuration sources. Reason: {ex.Message}");
    }
}

var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
var serviceBusConnectionString = builder.Configuration.GetConnectionString("ServiceBus");

if (string.IsNullOrWhiteSpace(appInsightsConnectionString))
{
    throw new InvalidOperationException(
        "Missing Application Insights connection string. Ensure Key Vault secret 'ApplicationInsights--ConnectionString' exists.");
}

if (string.IsNullOrWhiteSpace(serviceBusConnectionString))
{
    throw new InvalidOperationException(
        "Missing Service Bus connection string. Ensure Key Vault secret 'ConnectionStrings--ServiceBus' exists.");
}

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("MassTransit", LogEventLevel.Warning)
        .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithSpan()
        .Enrich.WithProperty("Application", "FraudService.API")
        .Enrich.WithProperty("Service", "fraud-api")
        .WriteTo.Console()
        .WriteTo.ApplicationInsights(
            services.GetRequiredService<Microsoft.ApplicationInsights.TelemetryClient>(),
            TelemetryConverter.Traces);
});

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = appInsightsConnectionString;
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live", "ready"]);

var fraudServiceQueue = builder.Configuration["Messaging:Queues:FraudServiceQueue"] ?? "fraud-service";
var fraudCheckCompletedTopic = builder.Configuration["Messaging:Topics:FraudCheckCompletedTopic"] ?? "fraud-check-completed";

builder.Services.AddMassTransit(x =>
{
    // Prevent runtime creation of Fault<T> topics/subscriptions when consumers throw.
    // We rely on the _error queue and logs for operational visibility.
    x.AddConfigureEndpointsCallback((name, cfg) =>
    {
        cfg.PublishFaults = false;
    });

    x.AddConsumer<RunFraudCheckConsumer>()
        .ExcludeFromConfigureEndpoints();

    x.UsingAzureServiceBus((context, cfg) =>
    {
        cfg.Host(serviceBusConnectionString, h =>
        {
            h.TransportType = Azure.Messaging.ServiceBus.ServiceBusTransportType.AmqpWebSockets;
        });

        cfg.Message<BuildingBlocks.Contracts.Fraud.FraudCheckCompleted>(m => m.SetEntityName(fraudCheckCompletedTopic));

        cfg.ReceiveEndpoint(fraudServiceQueue, e =>
        {
            e.ConfigureConsumeTopology = false;
            e.UseInMemoryOutbox(context);
            e.ConfigureConsumer<RunFraudCheckConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var traceSampleRatio = builder.Configuration.GetValue<double?>("Observability:Tracing:SampleRatio") ?? 1.0;
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProvider =>
    {
        tracerProvider
            .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(traceSampleRatio)))
            .AddAspNetCoreInstrumentation(options =>
            {
                options.Filter = httpContext =>
                    !httpContext.Request.Path.StartsWithSegments("/health") &&
                    !httpContext.Request.Path.StartsWithSegments("/live") &&
                    !httpContext.Request.Path.StartsWithSegments("/ready");
            })
            .AddHttpClientInstrumentation()
            .AddSource("MassTransit")
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService("FraudService"));
    });

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapHealthChecks("/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.UseHttpsRedirection();

app.Run();
