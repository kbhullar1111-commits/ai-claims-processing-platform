extern alias azureidentity;

using NotificationService.Application.Interfaces;
using NotificationService.Application.Commands.CreateNotification;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NotificationService.Infrastructure.Messaging.Consumers;
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

var logDirectory = Path.Combine(builder.Environment.ContentRootPath, "logs");
Directory.CreateDirectory(logDirectory);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
        .MinimumLevel.Override("MassTransit", LogEventLevel.Warning)
        .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithSpan()
        .Enrich.WithProperty("Application", "NotificationService.API")
        .Enrich.WithProperty("Service", "notification-api")
        .WriteTo.Console()
        .WriteTo.File(
            Path.Combine(logDirectory, "notification-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            shared: true)
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

var notificationServiceQueue = builder.Configuration["Messaging:Queues:NotificationServiceQueue"] ?? "notification-service";

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ClaimSubmittedConsumer>()
        .ExcludeFromConfigureEndpoints();

    x.AddConsumer<RequestDocumentsConsumer>()
        .ExcludeFromConfigureEndpoints();

    x.UsingAzureServiceBus((context, cfg) =>
    {
        cfg.Host(serviceBusConnectionString, h =>
        {
            h.TransportType = Azure.Messaging.ServiceBus.ServiceBusTransportType.AmqpWebSockets;
        });

        cfg.ReceiveEndpoint(notificationServiceQueue, e =>
        {
            e.ConfigureConsumer<ClaimSubmittedConsumer>(context);
            e.ConfigureConsumer<RequestDocumentsConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

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
            .AddSource("MassTransit")
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
