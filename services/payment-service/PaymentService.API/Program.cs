extern alias azureidentity;

using Azure.Messaging.ServiceBus;
using PaymentService.Application;
using PaymentService.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Enrichers.Span;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Monitor.OpenTelemetry.Exporter;

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
        .Enrich.WithProperty("Application", "PaymentService.API")
        .Enrich.WithProperty("Service", "payment-api")
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

builder.Services.AddSingleton<IPaymentProcessor, PaymentProcessor>();

var serviceBusNamespace = builder.Configuration["ServiceBus:FullyQualifiedNamespace"];

if (string.IsNullOrWhiteSpace(serviceBusNamespace))
{
    throw new InvalidOperationException(
        "Missing Service Bus configuration. Set ServiceBus:FullyQualifiedNamespace in configuration (Key Vault, appsettings.json, or environment variables).");
}

builder.Services.Configure<PaymentMessagingOptions>(builder.Configuration.GetSection(PaymentMessagingOptions.SectionName));
builder.Services.AddSingleton(sp =>
{
    var configured = sp.GetRequiredService<IOptions<PaymentMessagingOptions>>().Value;

    var queueFromNestedSection = builder.Configuration["Messaging:Queues:PaymentServiceQueue"];
    var topicFromNestedSection = builder.Configuration["Messaging:Topics:PaymentProcessedTopic"];

    return new PaymentMessagingOptions
    {
        PaymentServiceQueue = string.IsNullOrWhiteSpace(queueFromNestedSection)
            ? configured.PaymentServiceQueue
            : queueFromNestedSection,
        PaymentProcessedTopic = string.IsNullOrWhiteSpace(topicFromNestedSection)
            ? configured.PaymentProcessedTopic
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

builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();

    try
    {
        return new ServiceBusClient(serviceBusNamespace!, defaultAzureCredential, clientOptions);
    }
    catch (Exception ex)
    {
        logger.LogError(ex,
            "Failed to create ServiceBusClient with Managed Identity using DefaultAzureCredential for namespace {ServiceBusNamespace}",
            serviceBusNamespace);

        throw new InvalidOperationException(
            "Unable to create the Service Bus client using Managed Identity. Check that the managed identity has access to the Service Bus namespace and that the DefaultAzureCredential configuration is correct.",
            ex);
    }
});

builder.Services.AddSingleton<PaymentProcessedPublisher>();
builder.Services.AddHostedService<PaymentMessagePump>();

var traceSampleRatio = builder.Configuration.GetValue<double?>("Observability:Tracing:SampleRatio") ?? 1.0;
builder.Services.AddOpenTelemetry()
    .UseAzureMonitor(options =>
    {
        options.ConnectionString = appInsightsConnectionString;
    })
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
                    .AddService("PaymentService"));
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

