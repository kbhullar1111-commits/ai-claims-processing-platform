using DocumentService.Application.Interfaces;
using DocumentService.Application.Commands;
using DocumentService.Infrastructure.Storage;
using DocumentService.Infrastructure.Messaging;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Options;
using Minio;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<ObjectStorageOptions>(
    builder.Configuration.GetSection("ObjectStorage"));

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

    var client = sp.GetRequiredService<IMinioClient>();

    return new MinioObjectStorage(client, options.Bucket);
});

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ObjectCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("claimsuser");
            h.Password("claimspassword");
        });

        cfg.ReceiveEndpoint("minio-object-created", e =>
        {
            e.Bind("minio");   // IMPORTANT
            e.ConfigureConsumer<ObjectCreatedConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var client = scope.ServiceProvider
        .GetRequiredService<IMinioClient>();

    var options = scope.ServiceProvider
        .GetRequiredService<IOptions<ObjectStorageOptions>>()
        .Value;

    await MinioBucketInitializer.EnsureBucketExists(
        client,
        options.Bucket);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

