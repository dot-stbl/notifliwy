---
description: Guidelines for Notifliwy event notification library - pipeline components, builders, conditions, mappers, steps, exporters, and diagnostic patterns
---

# notifliwy-development

Guidelines for working with Notifliwy - a .NET event notification library.

## Trigger Conditions

Use this skill when:
- Adding new notification pipelines
- Working with event-to-notification mapping
- Implementing conditions, mappers, steps, or exporters
- Configuring the notification server
- Debugging pipeline flow
- Adding OpenTelemetry instrumentation

## Core Concepts

### Pipeline Flow

```
Event → InputPipe → Connector → Sector → Condition → Mapper → Steps → Exporter
```

### Key Interfaces

| Interface | Method | Purpose |
|-----------|--------|---------|
| `IInputPipe<TEvent>` | `AcceptAsync()` → `IAsyncEnumerable<TEvent>` | Provides events |
| `INotificationCondition<TN,TE>` | `AllowItAsync()` | Optional filter |
| `INotificationMapper<TN,TE>` | `ConvertAsync()` | Required converter |
| `INotificationStep<TN>` | `AggregateAsync()` | Optional transform |
| `INotificationExporter<TN>` | `ThrowAsync()` | Final output |

## Builder Pattern

### Minimal Setup

```csharp
builder.Services.AddNotifliwyServer(serverBuilder =>
{
    serverBuilder.AddNotification<Notification, Event>(sector =>
    {
        sector.AddMapper<MyMapper>(); // Required!
    });
    serverBuilder.AddInMemoryInput();
});
```

### Full Pipeline

```csharp
serverBuilder.AddNotification<MyNotification, MyEvent>(sectorBuilder =>
{
    sectorBuilder.AddCondition<MyCondition>();     // Optional filter
    sectorBuilder.AddMapper<MyMapper>();            // Required
    sectorBuilder.WithPipeline(pipelineBuilder =>  // Optional steps
    {
        pipelineBuilder.AddStep<Step1>();
        pipelineBuilder.AddStep<Step2>();
    });
    sectorBuilder.AddExporter<MyExporter>();       // Final output
});
```

## Component Implementation

### Condition (Filter)

```csharp
public sealed class EvenNumberCondition : INotificationCondition<Notification, Event>
{
    public ValueTask<bool> AllowItAsync(Event input, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(input.Value % 2 == 0); // false stops pipeline
    }
}
```

### Mapper (Converter)

```csharp
public sealed class EventToNotificationMapper : INotificationMapper<Notification, Event>
{
    public ValueTask<Notification> ConvertAsync(Event input, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(new Notification
        {
            Value = input.Value * 2,
            ProcessedAt = DateTimeOffset.UtcNow
        });
    }
}
```

### Step (Transform)

```csharp
public sealed class EnrichmentStep : INotificationStep<Notification>
{
    public ValueTask<Notification> AggregateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        notification.Metadata["enriched"] = "true";
        return ValueTask.FromResult(notification);
    }
}
```

### Exporter (Output)

```csharp
public sealed class ConsoleExporter : INotificationExporter<Notification>
{
    public ValueTask ThrowAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Notification: {notification.Value}");
        return ValueTask.CompletedTask;
    }
}
```

## Primary Constructor Pattern

Parameters sorted shortest to longest (pyramid rule):

```csharp
// ✅ Correct
public sealed class SectorBlock<TNotification, TEvent>(
    IServiceProvider serviceProvider,
    ILogger<SectorBlock<TNotification, TEvent>> sectorLogger,
    INotificationConditionProcessor<TNotification, TEvent> conditionProcessor)

// ❌ Wrong - unsorted
public sealed class SectorBlock<TNotification, TEvent>(
    ILogger<SectorBlock<TNotification, TEvent>> logger,
    IServiceProvider serviceProvider)
```

## Pattern Matching

```csharp
// Check for null and enter block
if (serviceProvider.GetServices<T>() is not {} services || services.Length == 0)
{
    return Result.Failure("No services registered");
}

// Pattern match in conditions
if (result is { IsSuccess: true, Value: not null } success)
{
    await exporter.ThrowAsync(success.Value, cancellationToken);
}
```

## Structured Logging

```csharp
// ✅ Correct - structured logging
logger.LogDebug(
    "Event {EventHash} / {EventType} allowed, processing",
    inputEvent?.GetHashCode(),
    DiagnosticEventData<TEvent>.EventSeparation);

logger.LogInformation("Notification {NotificationType} exported", typeof(TNotification).Name);

logger.LogError(exception, "Notification sector failed with exception");

// ❌ Wrong - string interpolation
logger.LogInformation($"Event {event.Id} processed");
```

## Async Method Naming

All async methods MUST:
1. End with `Async` suffix
2. Have `CancellationToken` as last parameter

```csharp
// ✅ Correct
public async ValueTask<bool> AllowItAsync(Event input, CancellationToken cancellationToken = default)
public ValueTask<Notification> ConvertAsync(Event input, CancellationToken cancellationToken = default)

// ❌ Wrong
public async Task<bool> AllowIt(Event input)                    // missing Async
public ValueTask<Notification> ConvertAsync(CancellationToken ct, Event input)  // ct not last
```

## Diagnostic Patterns

### Activity Tracing

```csharp
using var activity = DiagnosticActivity.NotifliwySource.StartConnectorActivity<TEvent>();
activity?.SetStatus(ActivityStatusCode.Error);
activity.RecordException(exception);
```

### Meter Metrics

```csharp
DiagnosticMeter.InputCounter.Add(delta: 1, tagList: DiagnosticEventData<TEvent>.TagsBy);
```

## No Private Methods

Never create private methods. Use:
- **File static class** with extension methods
- **Local functions** inside methods

```csharp
// ✅ Extension method
file static class NotificationExtensions
{
    public static bool IsValid(this Notification notification) 
        => notification.Exporters.Count > 0;
}

// ✅ Local function
public async ValueTask ProcessAsync(Event input, CancellationToken cancellationToken)
{
    var validate = () => input.Value > 0;
    if (!validate()) return;
    // ...
}
```

## Lambda Parameters

Use meaningful names, never single letters:

```csharp
// ✅ Correct
notifications
    .Where(notif => notif.IsActive)
    .Select(mapper => mapper.OutputType)
    .ForEach(exporter => exporter.ThrowAsync(notif, cancellationToken));

// ❌ Wrong
notifications.Where(n => n.IsActive).Select(x => x.Type).ToList();
```

## Known Issues

### Bug: MultiplyServiceInstance.CheckoutInstanceAsync

The `CheckoutInstanceAsync` overload returning `void` always calls `multiplyAction` even when `IsSingle` is true. Must add `return` after `singleAction`.

**Location:** `src/libraries/Notifliwy/Related/MultiplyServiceInstance.cs`, lines 126-131

```csharp
// Current (buggy)
if (IsSingle && Single != null)
{
    await singleAction(Single);
}
await multiplyAction(Multiply!); // Always executes!

// Fixed
if (IsSingle && Single != null)
{
    await singleAction(Single);
    return; // Add this
}
await multiplyAction(Multiply!);
```

### Bug: Fire-and-forget Task.Run

`NotificationConnector` uses fire-and-forget `Task.Run` without awaiting. Exceptions are silently swallowed.

**Location:** `src/libraries/Notifliwy/Connectors/NotificationConnector.cs`, line 48

## Key Files Reference

| File | Purpose |
|------|---------|
| `src/libraries/Notifliwy/Builders/NotificationServerBuilder.cs` | Root builder for DI |
| `src/libraries/Notifliwy/Builders/NotificationSectorBuilder.cs` | Per-notification builder |
| `src/libraries/Notifliwy/Connectors/NotificationConnector.cs` | Background event processor |
| `src/libraries/Notifliwy/Contexts/NotificationSector.cs` | Event-to-notification mapper |
| `src/libraries/Notifliwy/Contexts/SectorBlock.cs` | Holds all pipeline components |
| `src/libraries/Notifliwy/Pipes/InMemory/InMemoryInputPipe.cs` | In-memory event source |
| `src/libraries/Notifliwy/Pipes/InMemory/InMemoryExportPipe.cs` | In-memory event export |
| `src/libraries/Notifliwy/Related/MultiplyServiceInstance.cs` | Multi-instance service holder |
| `src/libraries/Notifliwy/Diagnostic/DiagnosticActivity.cs` | Activity source |
| `src/libraries/Notifliwy/Diagnostic/DiagnosticMeter.cs` | Metrics meter |

## Anti-Patterns to Avoid

### String Interpolation in Logging

```csharp
// Wrong
logger.LogInformation($"Event {event.Id} processed");

// Correct
logger.LogInformation("Event {EventId} processed", event.Id);
```

### Missing CancellationToken

```csharp
// Wrong
public async Task ProcessAsync() { }

// Correct
public async Task ProcessAsync(CancellationToken cancellationToken = default) { }
```

### Non-Descriptive Parameter Names

```csharp
// Wrong
public ValueTask ConvertAsync(Event e, CancellationToken ct)

// Correct
public ValueTask ConvertAsync(Event inputEvent, CancellationToken cancellationToken)
```

### Underscore Private Fields

```csharp
// Wrong
private readonly string _someValue;

// Correct - use auto-property
public string SomeValue { get; }
```