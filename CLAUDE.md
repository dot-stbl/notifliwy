# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Test Commands

### Building the Solution
```bash
dotnet build notifliwy.sln
```

The solution targets multiple .NET frameworks: 6.0, 7.0, and 8.0. Build using `dotnet build` which handles multi-targeting automatically.

### Running Tests
```bash
# Run all unit tests
dotnet test test/Notifliwy.Units

# Run tests with coverage
dotnet test test/Notifliwy.Units --collect:"XPlat Code Coverage"

# Run a single test method
dotnet test test/Notifliwy.Units --filter "FullyQualifiedName~TestMethodName"

# Run tests matching a pattern
dotnet test test/Notifliwy.Units --filter "FullyQualifiedName~Namespace"
```

The test project uses xUnit with Moq and Shouldly for assertions and mocking.

### Running Benchmarks
```bash
dotnet run --project test/Notifliwy.Benchmark
```

Benchmarks use BenchmarkDotNet and target net8.0 with optimizations enabled.

### Running Samples

**In-Memory Sample:**
```bash
dotnet run --project samples/inmemory/Notifliwy.Sample.InMemory
```

**Kafka Sample (requires Kafka cluster):**
```bash
# Start Kafka and Jaeger with Docker Compose
docker-compose -f deploy/sample-kafka-compose.yml up -d

# Run the server
dotnet run --project samples/kafka/Notifliwy.Sample.Kafka.Server

# Run the sender (in a separate terminal)
dotnet run --project samples/kafka/Notifliwy.Sample.Kafka.Sender
```

## Architecture Overview

Notifliwy is a .NET library for event-driven architecture that implements a **pipeline-based event processing pattern**. The core concept is converting incoming events to notifications through configurable stages.

### Pipeline Flow

```
event -> inputPipe.AcceptAsync() -> connector -> sector.ProcessAsync()
-> condition -> mapper -> pipeline steps -> exporter
```

### Key Components

**1. Notification Server & Builders**
- `NotificationServerBuilder` - Root builder for configuring the server
- `NotificationSectorBuilder` - Configures event-to-notification mapping with conditions, mappers, steps, and exporters
- Registered via `AddNotifliwyServer()` extension method

**2. Notification Sector**
- Maps a specific `TEvent` type to a `TNotification` type
- Contains one or more pipelines for processing the notification
- Pipelines chain: the notification returned by one pipeline is passed as input to the next (whether they should instead run independently is tracked in #9)

**3. Pipeline Stages (in order of execution)**
- `IInputPipe<TEvent>` - Provides `IAsyncEnumerable<TEvent>` via `AcceptAsync()`
- `INotificationCondition<TNotification, TEvent>` - Optional filter (if returns false, processing stops)
- `INotificationMapper<TNotification, TEvent>` - Required: converts event to notification
- `INotificationStep<TNotification>` - Optional: aggregates/transforms notification (multiple can run in sequence)
- `INotificationExporter<TNotification>` - Final output handler

**4. NotificationConnector**
- Background service that bridges `IInputPipe<TEvent>` to all registered sectors
- Awaits `Parallel.ForEachAsync` over sectors (`MaxDegreeOfParallelism = ProcessorCount`) so multiple sectors process the same event concurrently
- Sector errors are logged and rethrown; the connector waits for all sectors before pulling the next event
- Integrates with `DiagnosticActivity` and `DiagnosticMeter` for observability

### Processing Model

- **Sectors** process events **in parallel** via `Parallel.ForEachAsync`
- **Pipelines within a sector** run **sequentially and chain** — each pipeline receives the previous pipeline's output (#9)
- Each sector creates its own DI scope per event

### Provider Pattern

Providers extend Notifliwy by implementing `IInputPipe<TEvent>` for specific message brokers:

**In-Memory Provider (Built-in)**
- Uses `IInMemoryEventExchange<TEvent>` with `System.Threading.Channels`
- Implements `InMemoryInputPipe<TEvent>` and `InMemoryExportPipe<TEvent>`
- Added via `serverBuilder.AddInMemoryInput()`

**Kafka Provider (MassTransit)**
- `KafkaConsumerPipe<TEvent>` implements MassTransit consumer
- Uses `InMemoryExportPipe` internally for buffering
- Added via `registrationConfigurator.AddNotifliwyPipe<TEvent>()` and `endpoint.ConfigureNotifliwyPipe(context)`

### Dependency Injection Patterns

- Services are registered using `NotificationServerBuilder` which wraps `IServiceCollection`
- `IInputPipe<TEvent>` and `IExportPipe<TEvent>` are typically registered per-event type
- `INotificationCondition`, `INotificationMapper`, `INotificationStep`, and `INotificationExporter` are registered as transient services
- The `NotificationConnector` is a hosted service that processes events continuously

### Diagnostic and Observability

- Uses `System.Diagnostics.ActivitySource` for tracing
- Custom `DiagnosticMeter` for metrics (counter for input events)
- `Notifliwy.OpenTelemetry.Instrumentation` provides OpenTelemetry integration
- Activity tags include event type information via `DiagnosticEventData<TEvent>.TagsBy`

## Known Issues

See [docs/BUGS.md](docs/BUGS.md) for current bugs, investigations, and fix status.

**No open issues** — all recorded findings are fixed or documented as intentional design.

## Coding Rules

Development follows C# rules defined in [`.claude/rules/`](.claude/rules/):

| Rule | Description |
|------|-------------|
| [naming.md](.claude/rules/naming.md) | Naming conventions, async naming, lambda params |
| [patterns.md](.claude/rules/patterns.md) | Primary constructors, pattern matching, no private methods |
| [xml-docs.md](.claude/rules/xml-docs.md) | XML documentation standards |
| [logging.md](.claude/rules/logging.md) | Structured logging patterns |
| [result.md](.claude/rules/result.md) | Result<T> pattern for error handling |

## Quick Reference

### Minimal Setup
```csharp
builder.Services.AddNotifliwyServer(serverBuilder =>
{
    serverBuilder.AddNotification<MyNotification, MyEvent>(sector =>
    {
        sector.AddMapper<MyMapper>(); // Required!
    });
    serverBuilder.AddInMemoryInput();
});
```

### Interface Signatures
```csharp
// Condition - returns false to stop pipeline
INotificationCondition<TN, TE>: AllowItAsync(TE input, CT ct) -> ValueTask<bool>

// Mapper - required converter
INotificationMapper<TN, TE>: ConvertAsync(TE input, CT ct) -> ValueTask<TN>

// Step - optional transform
INotificationStep<TN>: AggregateAsync(TN notif, CT ct) -> ValueTask<TN>

// Exporter - final output
INotificationExporter<TN>: ThrowAsync(TN notif, CT ct) -> ValueTask
```

### Key Conventions

- **Fluent API**: All configuration uses builder pattern with method chaining
- **Async Throughout**: All interfaces use `ValueTask` or `Task` for async operations
- **Multi-Targeting**: Core library targets .NET 6.0, 7.0 and 8.0 (netstandard2.1 is the `Synaptix.MassTransit.Kafka.Protobuf` add-on only)
- **DI-based**: All pipeline components must be registered in DI
- **Parallel Processing**: Sectors process events in parallel; pipelines within a sector run sequentially and chain into each other (#9)
- **Cancellation Support**: All async methods accept `CancellationToken`
- **Primary Constructors**: Use C# 12 primary constructor syntax

## Project Structure

```
src/
├── libraries/Notifliwy/           # Core library (multi-targeted)
│   ├── Builders/                   # Fluent API builders
│   ├── Connectors/                 # NotificationConnector
│   ├── Pipes/                      # IInputPipe, IExportPipe implementations
│   ├── Contexts/                   # SectorBlock, INotificationSector
│   ├── Conditions/                # INotificationCondition
│   ├── Mapper/                    # INotificationMapper
│   ├── Steps/                     # INotificationStep
│   ├── Exporters/                 # INotificationExporter
│   └── Diagnostic/                # Activity and Meter support
├── providers/Notifliwy.Provider.MassTransit.Kafka/
│   └── Extensions/                  # MassTransit/Kafka integration
└── diagnostic/Notifliwy.OpenTelemetry.Instrumentation/
    └── Extensions/                 # OpenTelemetry integration

samples/
├── inmemory/                       # Simple in-memory sample
└── kafka/                          # Distributed Kafka sample (server, sender)
```