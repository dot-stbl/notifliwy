using Confluent.Kafka;
using MassTransit;
using Notifliwy.Dependency;
using Notifliwy.Provider.MassTransit.Kafka.Extensions;
using Notifliwy.Sample.Kafka;
using Notifliwy.Sample.Kafka.Server;

var builder = WebApplication.CreateBuilder(args);

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
                    
                    endpoint.ConfigureNotifliwyPipe(context);
                });
        });
    });
});

builder.Services.AddNotifliwyServer(serverBuilder =>
{
    serverBuilder.AddNotification<CatMeowNotification, CatMeowEvent>(sectorBuilder =>
    {
        sectorBuilder
            .AddMapper<CatMeowMapper>()
            .AddCondition<CatMeowCondition>();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.Run();