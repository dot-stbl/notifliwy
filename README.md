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