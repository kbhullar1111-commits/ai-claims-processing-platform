extern alias azureidentity;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using CustomerService.Application;
using CustomerService.Application.Customers.CreateCustomer;
using CustomerService.Application.Customers.GetCustomer;
using CustomerService.Application.Customers.GetCustomers;
using CustomerService.Application.Customers.UpdateCustomer;
using CustomerService.Infrastructure.Repositories;
using CustomerService.Infrastructure.Persistence;


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

var postgresConnectionString = builder.Configuration.GetConnectionString("CustomerPostgres")
    ?? throw new InvalidOperationException("Connection string 'CustomerPostgres' is required.");

var npgsqlDataSourceBuilder = new NpgsqlDataSourceBuilder(postgresConnectionString);
npgsqlDataSourceBuilder.EnableDynamicJson();
var npgsqlDataSource = npgsqlDataSourceBuilder.Build();

builder.Services.AddDbContext<CustomerDbContext>(options =>
{
    options.UseNpgsql(
        npgsqlDataSource,
        npgsql =>
        {
            npgsql.MigrationsAssembly("CustomerService.Infrastructure");
        });
});

builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerUnitOfWork, CustomerUnitOfWork>();
builder.Services.AddScoped<CreateCustomerHandler>();
builder.Services.AddScoped<GetCustomerQueryHandler>();
builder.Services.AddScoped<GetCustomersQueryHandler>();
builder.Services.AddScoped<UpdateCustomerHandler>();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<CustomerDatabaseHealthCheck>("postgres", tags: ["ready"]);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();


var app = builder.Build();

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


internal sealed class CustomerDatabaseHealthCheck(CustomerDbContext dbContext) : IHealthCheck
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