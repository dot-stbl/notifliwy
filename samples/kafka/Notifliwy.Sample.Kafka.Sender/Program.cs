using MassTransit;
using Notifliwy.Sample.Kafka;
using Notifliwy.Sample.Kafka.Sender;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(configurator =>
{
    configurator.UsingInMemory((context, factoryConfigurator) =>
    {
        factoryConfigurator.ConfigureEndpoints(context);
    });
    
    configurator.AddRider(configure: registrationConfigurator =>
    {
        registrationConfigurator.AddProducer<CatMeowEvent>(topicName: "meow.event");
        
        registrationConfigurator.UsingKafka(configure: (_, factoryConfigurator) =>
        {
            factoryConfigurator.SetSerializationFactory(new ProtobufKafkaSerializerFactory());
            
            factoryConfigurator.Host(server: "localhost:9092");
            
            factoryConfigurator.TopicEndpoint<CatMeowEvent>(
                groupId: "meow-group",
                topicName: "meow.event",
                configure: endpoint =>
                {
                    endpoint.CreateIfMissing(configure: options =>
                    {
                        options.ReplicationFactor = 1;
                    });
                });
        });
    });
});

builder.Services.AddHostedService<MeowService>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();

namespace Notifliwy.Sample.Kafka.Sender
{
    public class MeowService(IServiceScopeFactory scopeFactory) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await using var scope = scopeFactory.CreateAsyncScope();

            var topicProducer = scope.ServiceProvider.GetRequiredService<ITopicProducer<CatMeowEvent>>();
        
            while (!stoppingToken.IsCancellationRequested)
            {
                await topicProducer.Produce(
                    cancellationToken: stoppingToken, 
                    message: new CatMeowEvent
                    {
                        Name = Random.Shared.Next(0, 10) > 5 ? "Bob" : "Yuki",
                        KittyMean = Random.Shared.Next(0, 10) > 5 ? "i'm strong kitty" : "give food, stupid human"
                    });

                await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
            }
        }
    }
}