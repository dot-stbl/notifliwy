# Notifliwy — Full Documentation

## Overview

Notifliwy is a .NET library for event-driven architecture that implements a **pipeline-based event processing pattern**. It converts incoming events to notifications through configurable stages using a fluent builder API.

---

## Architecture Deep-Dive

### Pipeline Flow

```
Event → InputPipe → Connector → Sector → Condition → Mapper → Steps → Exporter
```

Each stage has a specific responsibility:

| Stage | Interface | Purpose | Required |
|-------|-----------|---------|----------|
| **Input** | `IInputPipe<TEvent>` | Provides `IAsyncEnumerable<TEvent>` via `AcceptAsync()` | Yes |
| **Condition** | `INotificationCondition<TN, TE>` | Optional filter — pipeline stops if returns `false` | No |
| **Mapper** | `INotificationMapper<TN, TE>` | Required converter — transforms event to notification | **Yes** |
| **Steps** | `INotificationStep<TN>` | Optional transforms (multiple run sequentially) | No |
| **Exporter** | `INotificationExporter<TN>` | Final output handler | No |

### Component Responsibilities

**NotificationServerBuilder**
- Root builder for DI registration
- Entry point: `AddNotifliwyServer()`

**NotificationSectorBuilder**
- Configures mapping for a specific `TEvent` → `TNotification`
- Methods: `AddCondition()`, `AddMapper()`, `WithPipeline()`, `AddExporter()`

**NotificationSector**
- Executes when event passes through the connector
- Creates DI scope and delegates to SectorBlock

**SectorBlock**
- Holds resolved instances of all pipeline components
- Orchestrates the actual processing flow

**NotificationConnector**
- Background service (`BackgroundService`)
- Bridges `IInputPipe` to all registered sectors
- Uses `Parallel.ForEachAsync` for concurrent sector processing

### Processing Model

- **Sectors process events in parallel** — multiple sectors can handle the same event concurrently
- **Pipelines within a sector run sequentially** — steps execute in order
- **Each event gets its own DI scope** — ensures fresh instances per event

---

## Usage Patterns

### Minimal Setup

```csharp
builder.Services.AddNotifliwyServer(serverBuilder =>
{
    serverBuilder.AddNotification<NeedNotification, InputEvent>(sectorBuilder =>
    {
        sectorBuilder.AddMapper<InputNeedNotificationMapper>(); // Required!
    });

    serverBuilder.AddInMemoryInput(); // Built-in in-memory provider
});
```

### Full Pipeline Setup

```csharp
serverBuilder.AddNotification<MyNotification, MyEvent>(sectorBuilder =>
{
    sectorBuilder.AddCondition<MyCondition>();       // Optional filter
    sectorBuilder.AddMapper<MyMapper>();              // Required
    sectorBuilder.WithPipeline(pipelineBuilder =>    // Optional steps
    {
        pipelineBuilder.AddStep<Step1>();
        pipelineBuilder.AddStep<Step2>();
    });
    serverBuilder.AddExporter<MyExporter>();          // Final output
});
```

### Multiple Exporters (Fan-Out)

Multiple exporters receive the same notification:

```csharp
sectorBuilder.AddExporter<EmailExporter>();
sectorBuilder.AddExporter<SmsExporter>();
sectorBuilder.AddExporter<WebhookExporter>();
```

### Multiple Sectors (Same Event)

One event can trigger multiple notifications:

```csharp
serverBuilder.AddNotification<EmailNotification, OrderEvent>(sector => {
    sector.AddMapper<OrderToEmailMapper>();
    sector.AddExporter<EmailExporter>();
});

serverBuilder.AddNotification<SmsNotification, OrderEvent>(sector => {
    sector.AddMapper<OrderToSmsMapper>();
    sector.AddExporter<SmsExporter>();
});
```

---

## Component Implementation Guide

### Condition (Optional Filter)

Stops pipeline if `AllowItAsync` returns `false`:

```csharp
public sealed class EvenNumberCondition : INotificationCondition<Notification, Event>
{
    public ValueTask<bool> AllowItAsync(Event input, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(input.Value % 2 == 0);
    }
}
```

### Mapper (Required Converter)

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

### Step (Optional Transform)

Multiple steps execute sequentially:

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

### Exporter (Final Output)

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

---

## Providers & Packages

### Built-in Provider

| Provider | Description |
|----------|-------------|
| In-Memory | Uses `System.Threading.Channels`, added via `AddInMemoryInput()` |

### External Packages

| Package | Version | Description |
|---------|---------|-------------|
| `Notifliwy.Provider.MassTransit.Kafka` | 3.1.0 | Kafka consumer via MassTransit |
| `Synaptix.MassTransit.Kafka.Protobuf` | 2.1.0 | Protobuf serializer/deserializer |
| `Notifliwy.OpenTelemetry.Instrumentation` | 3.0.0 | OpenTelemetry tracing/metrics |

---

## Observability

### Activity Tracing

```csharp
using var activity = DiagnosticActivity.NotifliwySource.StartConnectorActivity<TEvent>();
activity?.SetStatus(ActivityStatusCode.Error);
activity.RecordException(exception);
```

### Metrics

```csharp
DiagnosticMeter.InputCounter.Add(delta: 1, tagList: DiagnosticEventData<TEvent>.TagsBy);
```

Available metrics:
- `notifliwy.server.event.count` — Number of events accepted
- `notifliwy.server.sector.count` — Number of events with final notification processing

---

## Known Issues

See [docs/BUGS.md](docs/BUGS.md) for detailed bug reports and fix status.

| Bug | Severity | Status |
|----|----------|--------|
| #1 MultiplyServiceInstance.CheckoutInstanceAsync calls multiplyAction after singleAction | High | FIXED |
| #2 Fire-and-forget Task.Run in NotificationConnector | Medium | DOCUMENTED (by design) |
| #3 Duplicate ConnectorsBuilder for same TEvent | Low | NOT A BUG (by design) |
| #4 EnumerableExtensions.AggregateAsync uses Task instead of ValueTask | Medium | OPEN |

**Priority fix:** Bug #4 — `AggregateAsync` should use `ValueTask` for consistency and performance.

---

## Key Files Reference

| File | Purpose |
|------|---------|
| `src/libraries/Notifliwy/Builders/NotificationServerBuilder.cs` | Root builder for DI registration |
| `src/libraries/Notifliwy/Builders/NotificationSectorBuilder.cs` | Per-notification configuration |
| `src/libraries/Notifliwy/Connectors/NotificationConnector.cs` | Background event processor |
| `src/libraries/Notifliwy/Contexts/NotificationSector.cs` | Event-to-notification mapper |
| `src/libraries/Notifliwy/Contexts/SectorBlock.cs` | Pipeline orchestration |
| `src/libraries/Notifliwy/Pipes/InMemory/InMemoryInputPipe.cs` | In-memory event source |
| `src/libraries/Notifliwy/Pipes/InMemory/InMemoryExportPipe.cs` | In-memory event export |
| `src/libraries/Notifliwy/Pipes/InMemory/InMemoryEventExchange.cs` | Channel-based queue |
| `src/libraries/Notifliwy/Related/MultiplyServiceInstance.cs` | Multi-instance service holder |
| `src/libraries/Notifliwy/Diagnostic/DiagnosticActivity.cs` | Activity source for tracing |
| `src/libraries/Notifliwy/Diagnostic/DiagnosticMeter.cs` | Metrics meter |

---

## Commands

```bash
# Build the solution
dotnet build notifliwy.sln

# Run all unit tests
dotnet test test/Notifliwy.Units

# Run tests with coverage
dotnet test test/Notifliwy.Units --collect:"XPlat Code Coverage"

# Run a single test
dotnet test test/Notifliwy.Units --filter "FullyQualifiedName~TestName"

# Run benchmarks
dotnet run --project test/Notifliwy.Benchmark

# Run in-memory sample
dotnet run --project samples/inmemory/Notifliwy.Sample.InMemory

# Run Kafka sample (requires Kafka cluster)
docker-compose -f deploy/sample-kafka-compose.yml up -d
dotnet run --project samples/kafka/Notifliwy.Sample.Kafka.Server
dotnet run --project samples/kafka/Notifliwy.Sample.Kafka.Sender
```

---

## Development Rules

Coding standards are defined in [`.claude/rules/`](.claude/rules/):

- [naming.md](.claude/rules/naming.md) — Naming conventions
- [patterns.md](.claude/rules/patterns.md) — Primary constructors, pattern matching
- [xml-docs.md](.claude/rules/xml-docs.md) — XML documentation
- [logging.md](.claude/rules/logging.md) — Structured logging
- [result.md](.claude/rules/result.md) — Result<T> pattern