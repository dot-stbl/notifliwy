# Notifliwy - Fluent Event Notification Library for .NET

![banner](https://raw.githubusercontent.com/dot-stbl/notifliwy/refs/heads/master/contents/repo.banner.png)

---

**Notifliwy** is a _powerful_ .NET library designed 
to **simplify** **event-driven** architecture implementation with a clean, `fluent API`. 
It provides end-to-end support for event notifications - 
from declaration to processing - using an intuitive **builder** pattern that makes complex 
workflows easy to configure.

---

## Key Features

* 🚀 **Fluent** builder API for seamless configuration

* 🧩 **Modular pipeline** components for event processing

* ⏱️ Built-in support for async processing

* 📦 **Dependency Injection** ready

* 📊 **Metrics** and **diagnostics** out of the box

## Stages of event processing

> `event` -> `connector<event>` -> `handler.executor<event>` 
> -> `notification.handler<notification, event>` -> `condition<notification, event>` 
> -> `mapper<notification, event>` -> `exporter<notification>`

1. `InputPipe<TEvent>` - input service, `AcceptAsync` return `IAsyncEnumerable` with assigned **events**

2. (**Internal**) `NotificationConnector<TEvent>` - the main handler and proxy for incoming events. 
Transmits and manages event handlers

3. `INotificationCondition<TNotification, TEvent>` - _Add-on element_, the service performs the check of the event and 
here it is decided whether it goes further along the pipeline or not

4. `INotificationMapper<TNotification, TEvent>` - Also _addable element_, conversion service of the received event 
that has passed all the added checks

5. `INotificaitonExporter<TNotification>` - Post send service when an event has been converted into a 
notification and is ready to be sent across the entire Notifliwy pipeline

---

## Fast start

To add a `Notifliwy` server, you need to add it via `DI`. And in the most minimal version, so that it can 
receive data from the hosted application itself, you need to add the _following steps_ in the **builder**

```csharp
//Add server to hosted application in service collection
builder.Services.AddNotifliwyServer(serverBuilder =>
{
    //Assign event to notification for Notifliwy server
    serverBuilder.AddNotification<NeedNotification, InputEvent>(sectorBuilder =>
    {
        sectorBuilder.AddMapper<InputNeedNotificationMapper>(); //Add to notification pipeline
    });
    
    serverBuilder.AddInMemoryInput(); //Add global for all events, inmemory pipe handling
});

//...

public record InputEvent : IEvent;
public record NeedNotification : IEvent;

public class InputNeedNotificationMapper : INotificationMapper<NeedNotification, InputEvent>
{
    /// <inheritdoc />
    public ValueTask<NeedNotification> ConvertAsync(
        InputEvent inputEvent, 
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(new NeedNotification
        {
            //... cast event to notification
        });
    }
}
```

### Providers

Notifliwy provides the main providers for the internal event pipeline.

| Provider                             | Version | Description                                        |
|--------------------------------------|---------|----------------------------------------------------|
| Notifliwy.Provider.MassTransit.Kafka | 1.2.0   | Notifliwy Consumers provider for MassTransit/Kafka |

### Additional package

Additional libraries adding functionality to the Notifliwy server or related logic with business ownership.

| Package                                 | Version | Description                                                  |
|-----------------------------------------|---------|--------------------------------------------------------------|
| Synaptix.MassTransit.Kafka.Protobuf     | 2.1.0   | `Kafka/MassTransit` protobuf **serializer**/**deserializer** |
| Notifliwy.OpenTelemetry.Instrumentation | 1.2.0   | **OpenTelemetry** instruments extensions for `Notifliwy`     |

## Project

This project comes with an [GNU3.0](LICENSE). Contact the `.stbl` group.