using DocumentService.Application.Interfaces;
using DocumentService.Application.Commands;
using DocumentService.Infrastructure.Storage;
using DocumentService.Infrastructure.Messaging;
using MediatR;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Minio;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Events;
using Serilog.Enrichers.Span;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);
var blobStorageConnectionString = builder.Configuration.GetConnectionString("BlobStorage");
var useAzureBlobStorage = !string.IsNullOrWhiteSpace(blobStorageConnectionString);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    var seqEnabled = context.Configuration.GetValue<bool>("Observability:Seq:Enabled");
    var seqUrl = context.Configuration["Observability:Seq:Url"];

    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithSpan()
        .Enrich.WithProperty("Application", "DocumentService.API")
        .Enrich.WithProperty("Service", "document-api")
        .WriteTo.Console()
        .WriteTo.ApplicationInsights(
            services.GetRequiredService<Microsoft.ApplicationInsights.TelemetryClient>(),
            TelemetryConverter.Traces);
});

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddDbContext<DocumentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<DocumentDatabaseHealthCheck>("postgres", tags: ["ready"]);

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
        typeof(GenerateUploadUrlCommand).Assembly);
});

if (!useAzureBlobStorage)
{
    builder.Services.Configure<ObjectStorageOptions>(
    builder.Configuration.GetSection("ObjectStorage"));
    builder.Services.Configure<RabbitMqOptions>(
        builder.Configuration.GetSection(RabbitMqOptions.SectionName));
        
    builder.Services.AddSingleton<IMinioClient>(sp =>
    {
        var options = sp
            .GetRequiredService<IOptions<ObjectStorageOptions>>()
            .Value;

        return new MinioClient()
            .WithEndpoint(options.Endpoint)
            .WithCredentials(options.AccessKey, options.SecretKey)
            .WithSSL(options.UseSsl)
            .Build();
    });
}

// builder.Services.AddScoped<IObjectStorage>(sp =>
// {
//     var options = sp
//         .GetRequiredService<IOptions<ObjectStorageOptions>>()
//         .Value;

//     // Use a separate client for presigned URLs pointing at the public endpoint
//     // so browsers can reach MinIO directly (not the internal Docker hostname).
//     var publicEndpoint = options.PublicEndpoint ?? options.Endpoint;
//     var presignClient = new MinioClient()
//         .WithEndpoint(publicEndpoint)
//         .WithCredentials(options.AccessKey, options.SecretKey)
//         .WithSSL(options.UseSsl)
//         .Build();

//     return new MinioObjectStorage(presignClient, options.Bucket);
// });

builder.Services.AddScoped<IObjectStorage>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    var connectionString =
        config.GetConnectionString("BlobStorage");

    var containerName =
        config["Storage:ContainerName"];

    return new AzureBlobObjectStorage(
        connectionString!,
        containerName!);
});

// The custom messaging flow is split into two background services:
// 1. ObjectCreatedConsumer ingests raw MinIO notifications and writes document + outbox rows.
// 2. OutboxDispatcher reads pending outbox rows and publishes them to RabbitMQ.
if (!useAzureBlobStorage)
{
    builder.Services.AddHostedService<ObjectCreatedConsumer>();
    builder.Services.AddHostedService<OutboxDispatcher>();
    builder.Services.AddSingleton<RabbitPublisher>();
}

var traceSampleRatio = builder.Configuration.GetValue<double?>("Observability:Tracing:SampleRatio") ?? 1.0;
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
    ?? builder.Configuration["Observability:Otlp:Endpoint"];

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
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService("DocumentService"));

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracerProvider.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
            });
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter();
    });

var app = builder.Build();

if (!useAzureBlobStorage)
{
    using var scope = app.Services.CreateScope();
    var client = scope.ServiceProvider
        .GetRequiredService<IMinioClient>();

    var options = scope.ServiceProvider
        .GetRequiredService<IOptions<ObjectStorageOptions>>()
        .Value;

    var rabbitMqOptions = scope.ServiceProvider
        .GetRequiredService<IOptions<RabbitMqOptions>>()
        .Value;

    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("DocumentService.MinioStartup");

    const int maxAttempts = 6;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await MinioBucketInitializer.EnsureBucketConfigured(
                client,
                options.Bucket,
                rabbitMqOptions.MinioNotificationArn);
            break;
        }
        catch (Exception ex) when (ex is HttpRequestException || ex.InnerException is HttpRequestException)
        {
            if (attempt == maxAttempts)
                throw;

            logger.LogWarning(
                ex,
                "MinIO bucket initialization failed on attempt {Attempt}/{MaxAttempts}. Retrying...",
                attempt,
                maxAttempts);

            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");
app.MapHealthChecks("/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapControllers();

app.MapPrometheusScrapingEndpoint("/metrics");

app.Run();

internal sealed class DocumentDatabaseHealthCheck(DocumentDbContext dbContext) : IHealthCheck
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

