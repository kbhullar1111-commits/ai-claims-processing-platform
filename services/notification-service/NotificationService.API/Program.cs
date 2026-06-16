extern alias azureidentity;

using Azure.Messaging.ServiceBus;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Commands.CreateNotification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NotificationService.Infrastructure.Messaging;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Infrastructure.Persistence.Repositories;
using NotificationService.Infrastructure.Workers;
using NotificationService.Infrastructure.Senders;
using NotificationService.Infrastructure.Observability.Metrics;
using NotificationService.Infrastructure.Observability.Constants;
using Npgsql;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Events;
using Serilog.Enrichers.Span;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Monitor.OpenTelemetry.Exporter;

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
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
        .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithSpan()
        .Enrich.WithProperty("Application", "NotificationService.API")
        .Enrich.WithProperty("Service", "notification-api")
        .WriteTo.Console()
        .WriteTo.ApplicationInsights(
            services.GetRequiredService<Microsoft.ApplicationInsights.TelemetryClient>(),
            TelemetryConverter.Traces);
});

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = appInsightsConnectionString;
});

var postgresConnectionString = builder.Configuration.GetConnectionString("NotificationPostgres")
    ?? throw new InvalidOperationException("Connection string 'NotificationPostgres' is required.");

var npgsqlDataSourceBuilder = new NpgsqlDataSourceBuilder(postgresConnectionString);
npgsqlDataSourceBuilder.EnableDynamicJson();
var npgsqlDataSource = npgsqlDataSourceBuilder.Build();

builder.Services.Configure<NotificationDispatcherOptions>(
    builder.Configuration.GetSection("NotificationDispatcher"));

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(
        npgsqlDataSource,
        b => b.MigrationsAssembly("NotificationService.Infrastructure")
));

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<NotificationDatabaseHealthCheck>("postgres", tags: ["ready"]);

var serviceBusNamespace = builder.Configuration["ServiceBus:FullyQualifiedNamespace"];

if (string.IsNullOrWhiteSpace(serviceBusNamespace))
{
    throw new InvalidOperationException(
        "Missing Service Bus configuration. Set ServiceBus:FullyQualifiedNamespace in configuration (Key Vault, appsettings.json, or environment variables).");
}

builder.Services.Configure<NotificationMessagingOptions>(
    builder.Configuration.GetSection(NotificationMessagingOptions.SectionName));

builder.Services.AddSingleton(sp =>
{
    var configured = sp.GetRequiredService<IOptions<NotificationMessagingOptions>>().Value;

    var queueFromNestedSection = builder.Configuration["Messaging:Queues:NotificationServiceQueue"];
    var topicFromNestedSection = builder.Configuration["Messaging:Topics:ClaimSubmittedTopic"];
    var subscriptionFromNestedSection = builder.Configuration["Messaging:Subscriptions:ClaimSubmittedSubscription"];

    return new NotificationMessagingOptions
    {
        NotificationServiceQueue = string.IsNullOrWhiteSpace(queueFromNestedSection)
            ? configured.NotificationServiceQueue
            : queueFromNestedSection,
        ClaimSubmittedTopic = string.IsNullOrWhiteSpace(topicFromNestedSection)
            ? configured.ClaimSubmittedTopic
            : topicFromNestedSection,
        ClaimSubmittedSubscription = string.IsNullOrWhiteSpace(subscriptionFromNestedSection)
            ? configured.ClaimSubmittedSubscription
            : subscriptionFromNestedSection,
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
    var credential = new azureidentity::Azure.Identity.DefaultAzureCredential();

    try
    {
        return new ServiceBusClient(serviceBusNamespace!, credential, clientOptions);
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

builder.Services.AddHostedService<NotificationMessagePump>();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateNotificationCommand).Assembly);
});

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<INotificationSender, EmailSender>();
builder.Services.AddSingleton<INotificationMetrics, NotificationMetrics>();

builder.Services.AddHostedService<NotificationDispatcher>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
            .AddEntityFrameworkCoreInstrumentation()
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(TelemetryConstants.ServiceName));
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(TelemetryConstants.MeterName)
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation();
     });

var app = builder.Build();

await EnsureDatabaseIsReachableAsync(app.Services);

app.UseSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/health");
app.MapHealthChecks("/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapControllers();

app.Run();

static async Task EnsureDatabaseIsReachableAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

    var canConnect = await dbContext.Database.CanConnectAsync(cts.Token);
    if (!canConnect)
    {
        throw new InvalidOperationException(
            "Startup validation failed: Postgres is unreachable. Stopping service to avoid endless retry logging.");
    }
}

internal sealed class NotificationDatabaseHealthCheck(NotificationDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Postgres is reachable.")
                : HealthCheckResult.Unhealthy("Postgres is not reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Postgres health check failed.", ex);
        }
    }
}
