extern alias azureidentity;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using Npgsql;
using System.Text.Json.Serialization;
using Serilog;
using Serilog.Events;
using Serilog.Enrichers.Span;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using OpenTelemetry.Trace;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Resources;

using PolicyService.Application.Policies;
using PolicyService.Application.PolicyTypeDefinitions;
using PolicyService.Application.Abstractions;
using PolicyService.Infrastructure.Persistence;
using PolicyService.Infrastructure.Repositories;


var builder = WebApplication.CreateBuilder(args);

var keyVaultEndpoint = builder.Configuration["KeyVault:Url"];

if (!string.IsNullOrEmpty(keyVaultEndpoint))
{
    try
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultEndpoint),
            new azureidentity::Azure.Identity.DefaultAzureCredential());
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Key Vault is unavailable. Continuing with local configuration sources. Reason: {ex.Message}");
    }
}

var appInsightsConnectionString =
    builder.Configuration["ApplicationInsights:ConnectionString"];

if (string.IsNullOrWhiteSpace(appInsightsConnectionString))
{
    throw new InvalidOperationException(
        "Missing Application Insights connection string.");
}

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = appInsightsConnectionString;
});

var postgresConnectionString = builder.Configuration.GetConnectionString("PolicyPostgres")
    ?? throw new InvalidOperationException("Connection string 'PolicyPostgres' is required.");

var npgsqlDataSourceBuilder = new NpgsqlDataSourceBuilder(postgresConnectionString);
npgsqlDataSourceBuilder.EnableDynamicJson();
var npgsqlDataSource = npgsqlDataSourceBuilder.Build();

builder.Services.AddDbContext<PolicyDbContext>(options =>
{
    options.UseNpgsql(
        npgsqlDataSource
    );
});

builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields =
        HttpLoggingFields.RequestMethod |
        HttpLoggingFields.RequestPath |
        HttpLoggingFields.ResponseStatusCode |
        HttpLoggingFields.Duration;
});

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
        .Enrich.WithProperty("Application", "PolicyService.API")
        .Enrich.WithProperty("Service", "policy-service")
        .WriteTo.Console()
        .WriteTo.ApplicationInsights(
            services.GetRequiredService<
                Microsoft.ApplicationInsights.TelemetryClient>(),
            TelemetryConverter.Traces);
});

var traceSampleRatio =
    builder.Configuration.GetValue<double?>(
        "Observability:Tracing:SampleRatio") ?? 1.0;

builder.Services.AddOpenTelemetry()
    .UseAzureMonitor(options =>
    {
        options.ConnectionString = appInsightsConnectionString;
    })
    .WithTracing(tracerProvider =>
    {
        tracerProvider
            .SetSampler(
                new ParentBasedSampler(
                    new TraceIdRatioBasedSampler(traceSampleRatio)))
            .AddAspNetCoreInstrumentation(options =>
            {
                options.Filter = context =>
                    !context.Request.Path.StartsWithSegments("/health") &&
                    !context.Request.Path.StartsWithSegments("/live") &&
                    !context.Request.Path.StartsWithSegments("/ready");
            })
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(
                        serviceName: "PolicyService",
                        serviceVersion: "1.0.0"));
});

builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();
builder.Services.AddScoped<IPolicyTypeDefinitionRepository, PolicyTypeDefinitionRepository>();
builder.Services.AddScoped<IPolicyUnitOfWork, PolicyUnitOfWork>();
builder.Services.AddScoped<CreatePolicyCommandHandler>();
builder.Services.AddScoped<GetPolicyQueryHandler>();
builder.Services.AddScoped<GetPoliciesQueryHandler>();
builder.Services.AddScoped<UpdatePolicyCommandHandler>();
builder.Services.AddScoped<ValidatePolicyHandler>();
builder.Services.AddScoped<CreatePolicyTypeDefinitionHandler>();
builder.Services.AddScoped<GetPolicyTypeDefQueryHandler>();
builder.Services.AddScoped<GetPolicyTypeDefinitionsQueryHandler>();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<PolicyDatabaseHealthCheck>("postgres", tags: ["ready"]);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto
});

app.UseHttpLogging();

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

internal sealed class PolicyDatabaseHealthCheck(PolicyDbContext dbContext) : IHealthCheck
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