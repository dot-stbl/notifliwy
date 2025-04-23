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
using Synaptix.MassTransit.Kafka.Protobuf;

var builder = WebApplication.CreateBuilder(args);

builder.Logging
    .ClearProviders()
    .AddConsole();

builder.Services.AddDbContext<TempDbContext>(
    optionsAction: optionsBuilder =>
    {
        optionsBuilder
            .UseInMemoryDatabase(databaseName: "cat-meow.db")
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    });

builder.Services.AddMassTransit(configurator =>
{
    configurator.UsingInMemory((context, factoryConfigurator) =>
    {
        factoryConfigurator.ConfigureEndpoints(context);
    });
    
    configurator.AddRider(configure: registrationConfigurator =>
    {
        registrationConfigurator.AddNotifliwyPipe<CatMeowEvent>();
        
        registrationConfigurator.UsingKafka(configure: (context, factoryConfigurator) =>
        {
            factoryConfigurator.SetSerializationFactory(new ProtobufKafkaSerializerFactory());
            
            factoryConfigurator.Host(server: "localhost:9092");
            
            factoryConfigurator.TopicEndpoint<CatMeowEvent>(
                groupId: "meow-group",
                topicName: "meow.event",
                configure: endpoint =>
                {
                    endpoint.GroupInstanceId = "notifliwy-cns-01";
                    
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
    serverBuilder.AddNotification<CatMeowNotification, CatMeowEvent>(sectorBuilder =>
    {
        sectorBuilder.AddMapper<CatMeowMapper>();
        sectorBuilder.AddCondition<CatMeowCondition>();

        //independent pipelines for certain notifications
        sectorBuilder.WithPipeline(pipelineBuilder =>
        {
            pipelineBuilder.AddStep<ColorNotificationStep>();
            pipelineBuilder.AddStep<ClearNotificationStep>();
        });

        //second pipeline after first
        sectorBuilder.WithPipeline(pipelineBuilder =>
        {
            pipelineBuilder.AddStep<ConstantColorNotificationStep>();
        });

        //Custom exporters
        sectorBuilder.AddExporter<CatNotificationConsoleExporter>();
        sectorBuilder.AddExporter<CatNotificationDatabaseExporter>();
    });
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resourceBuilder =>
    {
        resourceBuilder
            .AddService(serviceName: "cat.kafka.sample.server")
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