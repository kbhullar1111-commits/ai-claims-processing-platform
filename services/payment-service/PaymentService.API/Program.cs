using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ProcessPaymentConsumer>()
        .Endpoint(e => e.Name = "payment-service");

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

var app = builder.Build();


app.UseHttpsRedirection();

app.Run();

