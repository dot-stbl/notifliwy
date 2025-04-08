# Notifliwy.Provider.MassTransit.Kafka

Provider configuring **MassTransit** communication with `ConnectorGroup` **Notifliwy**. 
Technical adds as consumer main providers on the `InputPipe` side

## Configuration

```csharp
configurator.AddRider(configure: registrationConfigurator =>
{
    registrationConfigurator.AddNotifliwyPipe<CatMeowEvent>(); //Add assigned Notifliwy consumer to event
    
    registrationConfigurator.UsingKafka(configure: (context, factoryConfigurator) =>
    {        
        factoryConfigurator.Host(server: "kafka-url...");
        
        factoryConfigurator.TopicEndpoint<CatMeowEvent>(
            groupId: "example-group",
            topicName: "example-name",
            configure: endpoint =>
            {
                //... another configuration    
                endpoint.ConfigureNotifliwyPipe(context); //Confugre Notifliwy consumer
            });
    });
});
```

And `Notifliwy` server configuration example:

```csharp
builder.Services.AddNotifliwyServer(serverBuilder =>
{
    serverBuilder.AddNotification<CatMeowNotification, CatMeowEvent>(sectorBuilder =>
    {
        sectorBuilder
            .AddMapper<CatMeowMapper>()
            .AddCondition<CatMeowCondition>();
    });
});
```

## Project

This project comes with an [GNU3.0](../../../LICENSE). Contact the `.stbl` group.