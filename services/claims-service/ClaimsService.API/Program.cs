extern alias azureidentity;

using ClaimsService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ClaimsService.Application.Interfaces;
using ClaimsService.Infrastructure.Repositories;
using ClaimsService.Infrastructure.Observability.Metrics;
using ClaimsService.Infrastructure.Observability.Constants;
using ClaimsService.Infrastructure.Messaging;
using MediatR;
using Npgsql;
using ClaimsService.Application;
using ClaimsService.Application.Commands;
using ClaimsService.Application.Sagas;
using BuildingBlocks.Contracts.Claims;
using BuildingBlocks.Contracts.Documents;
using BuildingBlocks.Contracts.Fraud;
using BuildingBlocks.Contracts.Payment;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Events;
using Serilog.Enrichers.Span;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
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
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
        .MinimumLevel.Override("MassTransit", LogEventLevel.Warning)
        .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithSpan()
        .Enrich.WithProperty("Application", "ClaimsService.API")
        .Enrich.WithProperty("Service", "claims-api")
        .WriteTo.Console()
        .WriteTo.ApplicationInsights(
            services.GetRequiredService<Microsoft.ApplicationInsights.TelemetryClient>(),
            TelemetryConverter.Traces);
});

builder.Services.AddControllers();
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = appInsightsConnectionString;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

builder.Services.AddDbContext<ClaimsDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"),
        b => b.MigrationsAssembly("ClaimsService.Infrastructure")
));

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<ClaimsDatabaseHealthCheck>("postgres", tags: ["ready"]);

builder.Services.Configure<ClaimProcessingSagaRoutingOptions>(
    builder.Configuration.GetSection("Messaging:Queues"));

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(SubmitClaimCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(RejectClaimCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(ApproveClaimCommand).Assembly);
});

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<ClaimStatusConsumer>()
        .ExcludeFromConfigureEndpoints();

    x.AddConsumer<DocumentUploadedBridgeConsumer>()
        .ExcludeFromConfigureEndpoints();

    x.AddEntityFrameworkOutbox<ClaimsDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
        o.QueryDelay = TimeSpan.FromSeconds(10);
    });

    x.AddSagaStateMachine<ClaimProcessingSagaStateMachine, ClaimProcessingSagaState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
            r.UsePostgres();

            r.AddDbContext<DbContext, ClaimsDbContext>((provider, options) =>
            {
                options.UseNpgsql(
                    provider.GetRequiredService<IConfiguration>()
                        .GetConnectionString("Postgres"));
            });
        });

    x.UsingAzureServiceBus((context, cfg) =>
    {
        var connectionString = serviceBusConnectionString;

        cfg.Host(connectionString);

        var claimsServiceQueue =
            builder.Configuration["Messaging:Queues:ClaimsServiceQueue"]
            ?? "claims-service";

        cfg.ReceiveEndpoint(claimsServiceQueue, e =>
        {
            e.ConfigureConsumer<ClaimStatusConsumer>(context);
        });

        cfg.UseMessageRetry(r =>
        {
            r.Handle<DbUpdateConcurrencyException>();
            r.Handle<Npgsql.PostgresException>(x => x.SqlState == "40001"); // serialization/tx conflicts
            r.Interval(5, TimeSpan.FromMilliseconds(500));
        });

      cfg.SubscriptionEndpoint(
            "claims-document-bridge",
            "document-uploaded-raw",
            e =>
            {

                e.UseRawJsonDeserializer(
                    RawSerializerOptions.AnyMessageType,
                    isDefault: true);

                e.ConfigureConsumer<DocumentUploadedBridgeConsumer>(context);
            });

                cfg.ConfigureEndpoints(context);
            });
        });

builder.Services.AddScoped<IClaimRepository, ClaimRepository>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<IEventPublisher, EventPublisher>();
builder.Services.AddSingleton<IClaimsMetrics, ClaimsMetrics>();

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

app.MapControllers();

app.Run();

internal sealed class ClaimsDatabaseHealthCheck(ClaimsDbContext dbContext) : IHealthCheck
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