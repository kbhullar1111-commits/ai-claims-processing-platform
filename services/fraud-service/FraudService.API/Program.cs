extern alias azureidentity;

using Azure.Messaging.ServiceBus;
using FraudService.Application;
using FraudService.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Enrichers.Span;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

var builder = WebApplication.CreateBuilder(args);

var defaultAzureCredential = new azureidentity::Azure.Identity.DefaultAzureCredential();

var keyVaultEndpoint = builder.Configuration["KeyVault:Url"];

if (!string.IsNullOrEmpty(keyVaultEndpoint))
{
    try
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultEndpoint),
            defaultAzureCredential);

        builder.Configuration.AddEnvironmentVariables();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Key Vault is unavailable. Continuing with local configuration sources. Reason: {ex.Message}");
    }
}

var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];

if (string.IsNullOrWhiteSpace(appInsightsConnectionString))
{
    throw new InvalidOperationException(
        "Missing Application Insights connection string. Ensure Key Vault secret 'ApplicationInsights--ConnectionString' exists.");
}

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
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

builder.Services.AddSingleton<IFraudCheckProcessor, FraudCheckProcessor>();

var serviceBusNamespace = builder.Configuration["ServiceBus:FullyQualifiedNamespace"];
var serviceBusConnectionString = builder.Configuration.GetConnectionString("ServiceBus");

if (string.IsNullOrWhiteSpace(serviceBusNamespace) && string.IsNullOrWhiteSpace(serviceBusConnectionString))
{
    throw new InvalidOperationException(
    "Missing Service Bus configuration. Set ConnectionStrings:ServiceBus (preferred) or ServiceBus:FullyQualifiedNamespace for Managed Identity.");
}

builder.Services.Configure<FraudCheckMessagingOptions>(builder.Configuration.GetSection(FraudCheckMessagingOptions.SectionName));

builder.Services.AddSingleton(sp =>
{
    var configured = sp.GetRequiredService<IOptions<FraudCheckMessagingOptions>>().Value;

    var queueFromNestedSection = builder.Configuration["Messaging:Queues:FraudServiceQueue"];
    var topicFromNestedSection = builder.Configuration["Messaging:Topics:FraudCheckCompletedTopic"];

    return new FraudCheckMessagingOptions
    {
        FraudServiceQueue = string.IsNullOrWhiteSpace(queueFromNestedSection)
            ? configured.FraudServiceQueue
            : queueFromNestedSection,
        FraudCheckCompletedTopic = string.IsNullOrWhiteSpace(topicFromNestedSection)
            ? configured.FraudCheckCompletedTopic
            : topicFromNestedSection,
        MaxConcurrentCalls = configured.MaxConcurrentCalls,
        PrefetchCount = configured.PrefetchCount,
        MaxAutoLockRenewalMinutes = configured.MaxAutoLockRenewalMinutes,
        HandlerRetryMaxAttempts = configured.HandlerRetryMaxAttempts,
        HandlerRetryBaseDelayMs = configured.HandlerRetryBaseDelayMs,
        HandlerRetryMaxDelaySeconds = configured.HandlerRetryMaxDelaySeconds,
        MaxDeliveryAttemptsBeforeDeadLetter = configured.MaxDeliveryAttemptsBeforeDeadLetter
    };
});

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

builder.Services.AddSingleton(_ =>
{
    if (!string.IsNullOrWhiteSpace(serviceBusConnectionString))
    {
        return new ServiceBusClient(serviceBusConnectionString, clientOptions);
    }

    return new ServiceBusClient(serviceBusNamespace!, defaultAzureCredential, clientOptions);
});

builder.Services.AddSingleton<FraudCheckPublisher>();
builder.Services.AddHostedService<FraudCheckMessagePump>();

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

app.Run();
