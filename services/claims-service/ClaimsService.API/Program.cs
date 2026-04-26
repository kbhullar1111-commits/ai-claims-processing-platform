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
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, loggerConfiguration) =>
{
    var seqEnabled = context.Configuration.GetValue<bool>("Observability:Seq:Enabled");
    var seqUrl = context.Configuration["Observability:Seq:Url"];

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
        .WriteTo.Console();

    if (seqEnabled && !string.IsNullOrWhiteSpace(seqUrl))
    {
        loggerConfiguration.WriteTo.Seq(seqUrl);
    }
});

builder.Services.AddControllers();
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
            r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
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
        var connectionString =
            builder.Configuration.GetConnectionString("ServiceBus");

        cfg.Host(connectionString);

        var claimsServiceQueue =
            builder.Configuration["Messaging:Queues:ClaimsServiceQueue"]
            ?? "claims-service";

        cfg.ReceiveEndpoint(claimsServiceQueue, e =>
        {
            e.ConfigureConsumeTopology = false;
            e.ConfigureConsumer<ClaimStatusConsumer>(context);
        });

        cfg.UseMessageRetry(r =>
        {
            r.Handle<PostgresException>(x => x.SqlState == "40001");
            r.Interval(3, TimeSpan.FromSeconds(1));
        });

        cfg.Publish<MarkClaimApproved>(x => x.Exclude = true);
        cfg.Publish<MarkClaimRejected>(x => x.Exclude = true);
        cfg.Publish<MarkClaimUnderReview>(x => x.Exclude = true);
        cfg.Publish<RequestDocuments>(x => x.Exclude = true);
        cfg.Publish<RunFraudCheck>(x => x.Exclude = true);
        cfg.Publish<ProcessPayment>(x => x.Exclude = true);

        // DO NOT add RabbitMQ raw bridge endpoint here yet
        // var documentUploadedExchange = builder.Configuration["Messaging:DocumentUploaded:ExchangeName"] ?? "document-uploaded";
        // var documentUploadedQueue = builder.Configuration["Messaging:DocumentUploaded:QueueName"] ?? "document-uploaded-bridge";

        // cfg.ReceiveEndpoint(documentUploadedQueue, e =>
        // {
        //     // Only the bridge endpoint understands the raw publisher contract.
        //     // The saga continues to consume the internal typed event contract.
        //     e.UseRawJsonDeserializer(RawSerializerOptions.AnyMessageType, isDefault: true);
        //     e.Bind(documentUploadedExchange);
        //     e.ConfigureConsumer<DocumentUploadedBridgeConsumer>(context);
        // });

      cfg.SubscriptionEndpoint(
            "claims-document-bridge",
            "document-uploaded-raw",
            e =>
            {
                e.ConfigureConsumeTopology = false;

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
                    .AddService(TelemetryConstants.ServiceName))
            .AddOtlpExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(TelemetryConstants.MeterName)
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter();
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

app.MapPrometheusScrapingEndpoint("/metrics");

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