using DocumentService.Application.Interfaces;
using DocumentService.Application.Commands;
using DocumentService.Infrastructure.Storage;
using DocumentService.Infrastructure.Messaging;
using MassTransit;
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
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, loggerConfiguration) =>
{
    var seqEnabled = context.Configuration.GetValue<bool>("Observability:Seq:Enabled");
    var seqUrl = context.Configuration["Observability:Seq:Url"];

    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .MinimumLevel.Warning()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "DocumentService.API")
        .Enrich.WithProperty("Service", "document-api")
        .WriteTo.Console();

    if (seqEnabled && !string.IsNullOrWhiteSpace(seqUrl))
    {
        loggerConfiguration.WriteTo.Seq(seqUrl);
    }
});

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
builder.Services.Configure<ObjectStorageOptions>(
    builder.Configuration.GetSection("ObjectStorage"));

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
        typeof(GenerateUploadUrlCommand).Assembly);
});

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

builder.Services.AddScoped<IObjectStorage>(sp =>
{
    var options = sp
        .GetRequiredService<IOptions<ObjectStorageOptions>>()
        .Value;

    // Use a separate client for presigned URLs pointing at the public endpoint
    // so browsers can reach MinIO directly (not the internal Docker hostname).
    var publicEndpoint = options.PublicEndpoint ?? options.Endpoint;
    var presignClient = new MinioClient()
        .WithEndpoint(publicEndpoint)
        .WithCredentials(options.AccessKey, options.SecretKey)
        .WithSSL(options.UseSsl)
        .Build();

    return new MinioObjectStorage(presignClient, options.Bucket);
});

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ObjectCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = builder.Configuration["RabbitMq:Host"] ?? "localhost";
        var rabbitUsername = builder.Configuration["RabbitMq:Username"] ?? "claimsuser";
        var rabbitPassword = builder.Configuration["RabbitMq:Password"] ?? "claimspassword";

        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(rabbitUsername);
            h.Password(rabbitPassword);
        });

        cfg.ReceiveEndpoint("minio-object-created", e =>
        {
            e.UseRawJsonDeserializer(RawSerializerOptions.AnyMessageType, isDefault: true);
            e.Bind("minio");   // IMPORTANT
            e.ConfigureConsumer<ObjectCreatedConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
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
            .AddSource("MassTransit")
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService("DocumentService"))
            .AddOtlpExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter();
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var client = scope.ServiceProvider
        .GetRequiredService<IMinioClient>();

    var options = scope.ServiceProvider
        .GetRequiredService<IOptions<ObjectStorageOptions>>()
        .Value;

    await MinioBucketInitializer.EnsureBucketConfigured(
        client,
        options.Bucket,
        "arn:minio:sqs::PRIMARY:amqp");
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

app.UseHttpsRedirection();

app.MapControllers();

app.MapPrometheusScrapingEndpoint("/metrics");

app.Run();

