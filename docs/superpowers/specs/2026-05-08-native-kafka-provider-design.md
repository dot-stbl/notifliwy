# Native Kafka Provider Design

**Date:** 2026-05-08
**Author:** Brainstorming
**Status:** Draft

## Overview

Add a new provider `Notifliwy.Provider.Kafka` that uses Confluent.Kafka as a native client (without MassTransit) to implement `IInputPipe<TEvent>` for consuming Kafka messages.

## Project Structure

```
src/providers/Notifliwy.Provider.Kafka/
├── Notifliwy.Provider.Kafka.csproj
├── README.md
├── Configuration/
│   └── ConfluentKafkaOptions.cs
├── Serializers/
│   ├── IKafkaMessageDeserializer.cs
│   └── KafkaMessageDeserializer.cs (default JSON impl)
├── Extensions/
│   └── KafkaInputExtensions.cs
├── Input/
│   ├── KafkaSingleConsumerPipe.cs  (IInputPipe<TEvent>)
│   └── KafkaBatchConsumerPipe.cs    (IInputPipe<TEvent>)
└── Internal/
    └── KafkaConsumerBase.cs
```

**Namespace:** `Notifliwy.Provider.Kafka`

## Dependencies

```xml
<PackageReference Include="Confluent.Kafka" Version="2.*"/>
<ProjectReference Include="..\..\libraries\Notifliwy\Notifliwy.csproj"/>
```

Target frameworks: `net8.0;net7.0;net6.0`

## ConfluentKafkaOptions

```csharp
public sealed class ConfluentKafkaOptions
{
    public IList<string> Brokers { get; set; } = new List<string>();
    public string Topic { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Earliest;
    public bool EnableAutoCommit { get; set; } = true;
}
```

## Interfaces

### IKafkaMessageDeserializer<TEvent>

```csharp
public interface IKafkaMessageDeserializer<TEvent>
{
    ValueTask<TEvent?> DeserializeAsync(byte[] data, CancellationToken cancellationToken);
}
```

### Default JSON Implementation

```csharp
public sealed class KafkaMessageDeserializer<TEvent> : IKafkaMessageDeserializer<TEvent>
{
    public ValueTask<TEvent?> DeserializeAsync(byte[] data, CancellationToken cancellationToken)
    {
        var result = JsonSerializer.Deserialize<TEvent>(data);
        return ValueTask.FromResult<TEvent?>(result);
    }
}
```

## API

### Registration Extensions

```csharp
// With explicit Func deserializer (priority)
public static NotificationServerBuilder AddConfluentKafkaInput<TEvent>(
    this NotificationServerBuilder builder,
    Func<byte[], TEvent> deserializer,
    Action<ConfluentKafkaOptions> configure)
    where TEvent : class;

// With DI deserializer (fallback)
public static NotificationServerBuilder AddConfluentKafkaInput<TEvent>(
    this NotificationServerBuilder builder,
    Action<ConfluentKafkaOptions> configure)
    where TEvent : class;

// Batch version
public static NotificationServerBuilder AddConfluentKafkaBatchInput<TEvent>(
    this NotificationServerBuilder builder,
    Func<byte[], TEvent> deserializer,
    Action<ConfluentKafkaOptions> configure)
    where TEvent : class;

public static NotificationServerBuilder AddConfluentKafkaBatchInput<TEvent>(
    this NotificationServerBuilder builder,
    Action<ConfluentKafkaOptions> configure)
    where TEvent : class;
```

### Usage

```csharp
builder.Services.AddNotifliwyServer(serverBuilder =>
{
    serverBuilder.AddNotification<MyNotification, MyEvent>(sector =>
    {
        sector.AddMapper<MyMapper>();
    });

    // Single message consumption
    serverBuilder.AddConfluentKafkaInput<MyEvent>(
        deserializer: bytes => JsonSerializer.Deserialize<MyEvent>(bytes),
        configure: options =>
        {
            options.Brokers = new[] { "localhost:9092" };
            options.Topic = "my-topic";
            options.GroupId = "my-group";
        });

    // Batch consumption
    serverBuilder.AddConfluentKafkaBatchInput<MyEvent>(
        deserializer: bytes => JsonSerializer.Deserialize<MyEvent>(bytes),
        configure: options =>
        {
            options.Brokers = new[] { "localhost:9092" };
            options.Topic = "my-topic-batch";
            options.GroupId = "my-batch-group";
        });
});
```

## Implementation Details

### KafkaSingleConsumerPipe<TEvent>

- Implements `IInputPipe<TEvent>`
- Calls `consumer.Consume(cancellationToken)` in a loop
- Deserializes message using provided `Func`
- Yields each event via `IAsyncEnumerable<TEvent>`
- On deserialization error: logs warning and continues

### KafkaBatchConsumerPipe<TEvent>

- Implements `IInputPipe<TEvent>`
- Uses `consumer.ConsumeBatch()` or accumulates messages
- Yields events in batches via `IAsyncEnumerable<TEvent>`
- Configurable batch size via options (optional)

### Error Handling

- Deserialization errors: log warning with message details, skip message, continue consuming
- Consumer errors: log error, attempt graceful recovery, continue

### Graceful Shutdown

- On cancellation request, finish current `Consume` call before stopping
- Consumer closes with `consumer.Close()`
- No in-flight message loss for single consume mode

## Follows Project Rules

- Primary constructors
- No private methods (use file static classes)
- Async naming throughout (`Async` suffix)
- Structured logging with named parameters
- XML documentation on all public members
