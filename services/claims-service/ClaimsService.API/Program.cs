using ClaimsService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ClaimsService.Application.Interfaces;
using ClaimsService.Infrastructure.Repositories;
using MediatR;
using ClaimsService.Application;
using ClaimsService.Application.Commands;
using MassTransit;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

builder.Services.AddDbContext<ClaimsDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"),
        b => b.MigrationsAssembly("ClaimsService.Infrastructure")
));

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(SubmitClaimCommand).Assembly);
});

builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<ClaimsDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
        o.QueryDelay = TimeSpan.FromSeconds(10);
    });

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

builder.Services.AddScoped<IClaimRepository, ClaimRepository>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<IEventPublisher, EventPublisher>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();