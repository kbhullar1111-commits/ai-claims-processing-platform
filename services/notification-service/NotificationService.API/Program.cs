using NotificationService.Application.Interfaces;
using NotificationService.Application.Commands.CreateNotification;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NotificationService.Infrastructure.Messaging.Consumers;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Infrastructure.Persistence.Repositories;
using NotificationService.Infrastructure.Workers;
using NotificationService.Infrastructure.Senders;
using Npgsql;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

var logDirectory = Path.Combine(builder.Environment.ContentRootPath, "logs");
Directory.CreateDirectory(logDirectory);
// Uncomment this block if Seq sink diagnostics are needed again.
// var selfLogPath = Path.Combine(logDirectory, "serilog-selflog.txt");
// var selfLogLock = new object();
//
// SelfLog.Enable(message =>
// {
//     lock (selfLogLock)
//     {
//         File.AppendAllText(selfLogPath, $"{DateTime.UtcNow:O} {message}{Environment.NewLine}");
//     }
// });

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "NotificationService.API")
    .Enrich.WithProperty("Service", "notification-api")
    .WriteTo.Console()
    // Uncomment this sink if Seq logging is needed again.
    // .WriteTo.Seq(
    //     serverUrl: "http://seq:80",
    //     bufferBaseFilename: Path.Combine(logDirectory, "seq-buffer"),
    //     batchPostingLimit: 100,
    //     period: TimeSpan.FromSeconds(2))
    .WriteTo.File(
        Path.Combine(logDirectory, "notification-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        shared: true)
    .CreateLogger();

builder.Host.UseSerilog();

var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' is required.");

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

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ClaimSubmittedConsumer>();

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

builder.Services.AddHostedService<NotificationDispatcher>();

var app = builder.Build();

// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
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

app.Run();

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
