extern alias azureidentity;

using DocumentService.Application.Interfaces;
using DocumentService.Application.Commands;
// using DocumentService.Infrastructure.Persistence; // Not used - no database operations
using DocumentService.Infrastructure.Storage;
using Serilog;
using Serilog.Events;
using Serilog.Enrichers.Span;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
// using Microsoft.EntityFrameworkCore; // Not used - no database operations
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;

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

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
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

// Commented out: Document service doesn't perform database operations
// builder.Services.AddDbContext<DocumentDbContext>(options =>
//     options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

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
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);
    // .AddCheck<DocumentDatabaseHealthCheck>("postgres", tags: ["ready"]); // Commented out - no database operations

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
        typeof(GenerateUploadUrlCommand).Assembly);
});

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
            // .AddEntityFrameworkCoreInstrumentation() // Commented out - no database operations
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService("DocumentService"));
    });

var app = builder.Build();

// if (app.Environment.IsDevelopment())
// {
    app.UseSwagger();
    app.UseSwaggerUI();
// }

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

app.Run();

// Commented out: Document service doesn't perform database operations
// internal sealed class DocumentDatabaseHealthCheck(DocumentDbContext dbContext) : IHealthCheck
// {
//     public async Task<HealthCheckResult> CheckHealthAsync(
//         HealthCheckContext context,
//         CancellationToken cancellationToken = default)
//     {
//         try
//         {
//             var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
//             return canConnect
//                 ? HealthCheckResult.Healthy("Postgres is reachable.")
//                 : HealthCheckResult.Unhealthy("Postgres is not reachable.");
//         }
//         catch (Exception ex)
//         {
//             return HealthCheckResult.Unhealthy("Postgres health check failed.", ex);
//         }
//     }
// }

