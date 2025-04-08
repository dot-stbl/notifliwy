# Synaptix.MassTransit.Kafka.Protobuf

Add-on for masstransit kafka with the addition of protobuf serialization and deserialization

## How to use

```csharp
//.. masstransit registration
registrationConfigurator.UsingKafka(configure: (_, factoryConfigurator) =>
{
    //...
    factoryConfigurator.SetSerializationFactory(new ProtobufKafkaSerializerFactory());
    //...
}
```

## Project

This project comes with an [GNU3.0](../../../LICENSE). Contact the `.stbl` group.