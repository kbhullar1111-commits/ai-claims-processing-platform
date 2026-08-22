using Azure.Core;
using Azure.Identity;

using ClaimsService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ClaimsService.Application.Interfaces;
using ClaimsService.Infrastructure.Identity;
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
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Monitor.OpenTelemetry.Exporter;
using System.Reflection;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

var keyVaultEndpoint = builder.Configuration["KeyVault:Url"];

if (!string.IsNullOrEmpty(keyVaultEndpoint))
{
    try
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultEndpoint),
            new DefaultAzureCredential());

        builder.Configuration.AddEnvironmentVariables();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Key Vault is unavailable. Continuing with local configuration sources. Reason: {ex.Message}");
    }
}

var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
var serviceBusConnectionString = builder.Configuration.GetConnectionString("ServiceBus");
var claimsPostgresConnectionString = builder.Configuration.GetConnectionString("ClaimsPostgres");

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

if (string.IsNullOrWhiteSpace(claimsPostgresConnectionString))
{
    throw new InvalidOperationException(
        "Missing Claims Postgres connection string. Ensure Key Vault secret 'ConnectionStrings--ClaimsPostgres' exists.");
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
        .MinimumLevel.Override("Microsoft.Identity.Client", LogEventLevel.Warning)
        .MinimumLevel.Override("Azure.Identity", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithSpan()
        .Enrich.WithProperty("Application", "ClaimsService.API")
        .Enrich.WithProperty("Service", "claims-api")
        .WriteTo.Console()
        .WriteTo.ApplicationInsights(
            services.GetRequiredService<Microsoft.ApplicationInsights.TelemetryClient>(),
            TelemetryConverter.Traces);
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter());
});

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
    claimsPostgresConnectionString,
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

    // Prevent runtime creation of Fault<T> topics/subscriptions when consumers throw.
    // We rely on the _error queue and logs for operational visibility.
    x.AddConfigureEndpointsCallback((name, cfg) =>
    {
        cfg.PublishFaults = false;
    });

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
                options.UseNpgsql(claimsPostgresConnectionString);
            });
        });

    x.UsingAzureServiceBus((context, cfg) =>
    {
        var connectionString = serviceBusConnectionString;

        cfg.Host(connectionString, h =>
        {
            h.TransportType = Azure.Messaging.ServiceBus.ServiceBusTransportType.AmqpWebSockets;
        });

        var claimsServiceQueue =
            builder.Configuration["Messaging:Queues:ClaimsServiceQueue"]
            ?? "claims-service";

        var paymentProcessedTopic =
            builder.Configuration["Messaging:Topics:PaymentProcessedTopic"]
            ?? "payment-processed";

        var claimSubmittedTopic =
            builder.Configuration["Messaging:Topics:ClaimSubmittedTopic"]
            ?? "claim-submitted";

        var documentUploadedTopic =
            builder.Configuration["Messaging:Topics:DocumentUploadedTopic"]
            ?? "document-uploaded";

        var fraudCheckCompletedTopic =
            builder.Configuration["Messaging:Topics:FraudCheckCompletedTopic"]
            ?? "fraud-check-completed";

        var sagaIngressQueue =
            builder.Configuration["Messaging:Subscriptions:SagaIngressQueue"]
            ?? "claim-processing-saga-state";

        cfg.Message<ClaimSubmitted>(m => m.SetEntityName(claimSubmittedTopic));
        cfg.Message<DocumentUploaded>(m => m.SetEntityName(documentUploadedTopic));
        cfg.Message<FraudCheckCompleted>(m => m.SetEntityName(fraudCheckCompletedTopic));
        cfg.Message<PaymentProcessed>(m => m.SetEntityName(paymentProcessedTopic));

        cfg.ReceiveEndpoint(claimsServiceQueue, e =>
        {
            e.PublishFaults = false;
            e.ConfigureConsumeTopology = false;
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
                e.PublishFaults = false;
                e.UseRawJsonDeserializer(
                    RawSerializerOptions.AnyMessageType,
                    isDefault: true);

                e.ConfigureConsumer<DocumentUploadedBridgeConsumer>(context);
            });

        cfg.ReceiveEndpoint(sagaIngressQueue, e =>
        {
            e.PublishFaults = false;
            e.ConfigureConsumeTopology = false;

            e.Subscribe<ClaimSubmitted>(claimSubmittedTopic);
            e.Subscribe<DocumentUploaded>(documentUploadedTopic);
            e.Subscribe<FraudCheckCompleted>(fraudCheckCompletedTopic);
            e.Subscribe<PaymentProcessed>(paymentProcessedTopic);

            e.ConfigureSaga<ClaimProcessingSagaState>(context);
        });
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddScoped<IClaimRepository, ClaimRepository>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<IEventPublisher, EventPublisher>();
builder.Services.AddSingleton<IClaimsMetrics, ClaimsMetrics>();

builder.Services.AddSingleton<TokenCredential>(
    new ManagedIdentityCredential(new ManagedIdentityCredentialOptions()));

builder.Services.AddTransient<CustomerServiceAuthenticationHandler>();

builder.Services.AddHttpClient<ICustomerClient, CustomerClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:CustomerService:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(5);
})
.AddHttpMessageHandler<CustomerServiceAuthenticationHandler>();

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

app.MapControllers();

app.Run();

static async Task EnsureDatabaseIsReachableAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

    var canConnect = await dbContext.Database.CanConnectAsync(cts.Token);
    if (!canConnect)
    {
        throw new InvalidOperationException(
            "Startup validation failed: Postgres is unreachable. Stopping service to avoid endless retry logging.");
    }
}

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