using Confluent.Kafka;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notifliwy.Dependency;
using Notifliwy.OpenTelemetry.Instrumentation.Extensions;
using Notifliwy.Provider.MassTransit.Kafka.Extensions;
using Notifliwy.Sample.Kafka;
using Notifliwy.Sample.Kafka.Server;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Logging
        .ClearProviders()
        .AddConsole();

builder.Services.AddDbContext<TempDbContext>(optionsBuilder =>
{
    optionsBuilder
            .UseInMemoryDatabase("cat-meow.db")
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

builder.Services.AddMassTransit(configurator =>
{
    configurator.UsingInMemory((context, factoryConfigurator) =>
    {
        factoryConfigurator.ConfigureEndpoints(context);
    });

    configurator.AddConfigureEndpointsCallback((_, endpointConfigurator) =>
    {
        endpointConfigurator.UseCircuitBreaker(breakerConfigurator =>
        {
            breakerConfigurator.ResetInterval = TimeSpan.FromSeconds(5);
        });
    });

    configurator.AddRider(registrationConfigurator =>
    {
        registrationConfigurator.AddNotifliwyPipe<CatMeowEvent>();

        registrationConfigurator.UsingKafka((context, factoryConfigurator) =>
        {
            factoryConfigurator.Host("localhost:9092");

            var id = Random.Shared.Next(0, 999);
            factoryConfigurator.TopicEndpoint<CatMeowEvent>(
                groupId: $"meow-group-{id}",
                topicName: "meow.event",
                configure: endpoint =>
                {
                    endpoint.GroupInstanceId = $"notifliwy-cns-{id}";

                    endpoint.AutoOffsetReset = AutoOffsetReset.Latest;
                    endpoint.SessionTimeout = TimeSpan.FromMilliseconds(45000);
                    endpoint.HeartbeatInterval = TimeSpan.FromMilliseconds(3000);

                    endpoint.CreateIfMissing();
                    endpoint.ConfigureNotifliwyPipe(context);
                });
        });
    });
});

builder.Services.AddNotifliwyServer(serverBuilder =>
{
    serverBuilder.AddSector<CatMeowSector>();
});

builder.Services.AddOpenTelemetry()
        .ConfigureResource(resourceBuilder =>
        {
            resourceBuilder
                    .AddService("cat.kafka.sample.server")
                    .AddEnvironmentVariableDetector()
                    .AddTelemetrySdk();
        })
        .WithTracing(providerBuilder =>
        {
            providerBuilder.AddNotifliwyServerInstrumentation();

            providerBuilder.AddOtlpExporter(options =>
            {
                options.Protocol = OtlpExportProtocol.Grpc;
                options.Endpoint = new Uri("http://localhost:4317");
            });
        });

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.Run();