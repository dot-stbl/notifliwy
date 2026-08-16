# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Test Commands

### Building the Solution
```bash
dotnet build notifliwy.sln
```

The core library targets .NET 6.0, 7.0 and 8.0; the mapping packages and all test projects target net8.0. Build using `dotnet build` which handles multi-targeting automatically.

### Running Tests
```bash
# Core unit tests (graph model, executor, registration, pipes)
dotnet test test/Notifliwy.Units

# Source generator tests
dotnet test test/Notifliwy.Generators.Tests

# Mapping adapter tests (Mapperly + Mapster)
dotnet test test/Notifliwy.Mapping.Tests

# Tests with coverage
dotnet test test/Notifliwy.Units --collect:"XPlat Code Coverage"

# Run a single test method
dotnet test test/Notifliwy.Units --filter "FullyQualifiedName~TestMethodName"
```

Test projects use xUnit with Shouldly for assertions.

### Running Benchmarks
```bash
# All benchmarks (BenchmarkDotNet drops into interactive filter selection)
dotnet run --project test/Notifliwy.Benchmark

# Compiled vs Scoped execution-path comparison, dry run
dotnet run --project test/Notifliwy.Benchmark -- --filter *CompiledVsScoped* --job dry
```

Benchmarks target net8.0 with optimizations enabled. Full BDN sessions rebuild in
Release — build `src/` with `-c Release` first so the `GeneratePackageOnBuild` pack
targets in `Notifliwy`/`Notifliwy.Generators` find their artifacts.

### Running Samples

**In-Memory Sample (config-class sector, Branch + Join, source-gen registration):**
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

Notifliwy is a .NET library for event-driven architecture that converts incoming events to notifications through a **typed sector graph** (`When → Map → (Transform | Branch | Join | Custom | Export)*`). Version 3.2 replaced the 3.1 linear fluent sector API — the old surface is removed, not deprecated (see docs/MIGRATION-3.2.md).

### Graph Flow

```
event -> inputPipe.AcceptAsync() -> connector -> sector.ProcessAsync()
-> SectorGraphExecutor: conditions (When) -> mapper (Map) -> node walk
   (Transform | Branch fan-out -> Join reduce | Custom | Export)
```

### Key Components

**1. Notification Server & Registration**
- `NotificationServerBuilder` - Root builder, registered via `AddNotifliwyServer()`
- `AddSector<TConfig>()` - Register a sector from its config class
- `AddSector<TNotification, TEvent>(graph => ...)` - Inline one-off sector (validated at registration time)
- `AddSectorsFromAssembly(assembly)` - Opt-in reflection fallback; logs a startup warning

**2. Sector Configuration Classes**
- `INotificationSectorConfig<TNotification, TEvent>` - A sector as a plain class: `Configure(ISectorGraphBuilder<...>)` describes the graph; registered in DI as transient (constructor dependencies allowed)
- `SectorExecution Execution` - `Auto` (default: compile when every node is compile-safe, else scoped with a logged reason) / `Compiled` (fail fast with `SectorCaptiveDependencyException` on scoped nodes) / `Scoped`
- `BranchPolicy? DefaultBranchPolicy` - Sector-level default for `Branch` fan-outs without their own policy; `null` means `FailFast`

**3. Graph Nodes (in execution order after `Map`)**
- `IInputPipe<TEvent>` - Provides `IAsyncEnumerable<TEvent>` via `AcceptAsync()` (transport, outside the graph)
- `INotificationCondition<TNotification, TEvent>` - `When` filter on the raw event; must be registered before `Map`; all must allow
- `INotificationMapper<TNotification, TEvent>` - Required exactly once, before all other nodes; class or inline lambda
- `INotificationTransform<TNotification>` - Sequential transform on the path (renamed from `INotificationStep` in 3.2)
- `Branch(...)` - Parallel fan-out; every branch sub-graph receives the *same* notification instance and shares the per-event DI scope
- `INotificationJoin<TNotification>` - Reduces branch outputs back into the main path; single-branch join is a passthrough
- `INotificationCustom<TNotification>` - Escape hatch (DI class or inline lambda) for transform-shaped behaviour
- `INotificationExporter<TNotification>` - Final delivery; valid at any path position; multiple exports run serially

**4. Graph Plan & Execution**
- `ISectorGraphBuilder<TNotification, TEvent>` / `SectorGraphBuilder<...>` - Fluent builder recording the graph; the plan is frozen and validated when built (acyclic by construction; exactly one `Map`, `When` before `Map`, `Join` only after `Branch`, every branch ends with an `Export`)
- `SectorGraphExecutor<TNotification, TEvent>` (internal) - Executes the plan on one of two paths chosen at startup:
  - **Compiled** - nodes resolved/constructed once (singleton-registered or stateless with a parameterless ctor), direct invokes, no per-event DI scope
  - **Scoped** - fresh DI scope per event, every node resolved from it
- `BranchPolicy` - `FailFast` (default; first fault rethrows after all branches are observed) / `BestEffort` (failed branches logged and skipped; the following `Join` receives only survivors)

**5. Source Generator (rides in the Notifliwy package)**
- `[assembly: NotifliwySectors]` - Marks an assembly; `Notifliwy.Generators` emits `Notifliwy.Generated.NotifliwySectorsRegistration.AddNotifliwySectors(this NotificationServerBuilder)` with one direct `AddSector<TConfig>()` per public/internal concrete config class — zero runtime reflection

**6. NotificationConnector**
- Background service bridging `IInputPipe<TEvent>` to all registered sectors
- Awaits `Parallel.ForEachAsync` over sectors (`MaxDegreeOfParallelism = ProcessorCount`) so multiple sectors process the same event concurrently
- Sector errors are logged and rethrown; the connector waits for all sectors before pulling the next event
- Integrates with `DiagnosticActivity` and `DiagnosticMeter` for observability

### Processing Model

- **Sectors** process events **in parallel** via `Parallel.ForEachAsync`
- **Nodes within a graph path** run **sequentially** in registration order; **branch sub-graphs** run in parallel under the node's `BranchPolicy`
- Fan-out shares one notification instance (transforms must return new instances, not mutate) and one per-event DI scope (a scoped node like a `DbContext` is not branch-isolated)
- The main-path notification between a `Branch` and a `Join` stays the pre-branch input
- Each sector creates its own DI scope per event (scoped path)

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

### Mapping Providers

Adapter packages plug external mappers into a graph `Map` node; the core stays mapper-agnostic:

- `Notifliwy.Mapping.Mapperly` - `IMapperlyNotificationMapping<TNotification, TEvent>` bridge contract + `MapperlyNotificationMapper<...>` adapter; `AddNotifliwyMapperlyMapping<...>()` registers it. Compile-time generated bodies.
- `Notifliwy.Mapping.Mapster` - `MapsterNotificationMapper<...>` wraps a compiled `TypeAdapterConfig` delegate; `AddNotifliwyMapsterMapping(...)` registers it.

### Dependency Injection Patterns

- Services are registered using `NotificationServerBuilder` which wraps `IServiceCollection`
- `IInputPipe<TEvent>` and `IExportPipe<TEvent>` are typically registered per-event type
- Sector config classes are transient (may take constructor dependencies); graph nodes are transient or stateless; the graph plan and executor are singletons
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
    serverBuilder.AddSector<MySector>(); // INotificationSectorConfig<MyNotification, MyEvent>
    serverBuilder.AddInMemoryInput();
});

public class MySector : INotificationSectorConfig<MyNotification, MyEvent>
{
    public void Configure(ISectorGraphBuilder<MyNotification, MyEvent> graph)
    {
        graph.Map<MyMapper>(); // Required, before all other nodes!
    }
}
```

### Graph Node Signatures
```csharp
// When - condition on the raw event; false on any -> event dropped
INotificationCondition<TN, TE>: AllowItAsync(TE input, CT ct) -> ValueTask<bool>

// Map - required converter, exactly once, first non-When node
INotificationMapper<TN, TE>: ConvertAsync(TE input, CT ct) -> ValueTask<TN>

// Transform - sequential notification-to-notification node
INotificationTransform<TN>: TransformAsync(TN notif, CT ct) -> ValueTask<TN>

// Join - reduces branch outputs back into the main path
INotificationJoin<TN>: JoinAsync(IReadOnlyList<TN> notifs, CT ct) -> ValueTask<TN>

// Custom - escape hatch for transform-shaped behaviour
INotificationCustom<TN>: InvokeAsync(TN notif, CT ct) -> ValueTask<TN>

// Export - delivery at the current path position
INotificationExporter<TN>: ThrowAsync(TN notif, CT ct) -> ValueTask
```

### Key Conventions

- **Graph API**: `ISectorGraphBuilder` — `When<T>()` / `Map<T>()` or `Map(lambda)` / `Transform<T>()` / `Branch(policy?, params branches)` / `Join<T>()` / `Custom<T>()` or `Custom(lambda)` / `Export<T>()`
- **Registration**: config classes (`AddSector<TConfig>()`) for hosts, inline lambdas for one-offs, `[assembly: NotifliwySectors]` + generated `AddNotifliwySectors()` for whole assemblies
- **Async Throughout**: All interfaces use `ValueTask` or `Task` for async operations
- **Multi-Targeting**: Core library targets .NET 6.0, 7.0 and 8.0; mapping packages net8.0
- **DI-based**: Graph nodes resolve from DI (transient/singleton) or are stateless classes with a parameterless constructor (compile-safe)
- **Execution Modes**: `SectorExecution.Auto|Compiled|Scoped` chosen at startup; compiled demands singleton-safe nodes
- **Cancellation Support**: All async methods accept `CancellationToken`
- **Primary Constructors**: Use C# 12 primary constructor syntax

## Project Structure

```
src/
├── libraries/Notifliwy/           # Core library (multi-targeted net6/7/8)
│   ├── Builders/                   # NotificationServerBuilder (AddSector surface)
│   ├── Graph/                      # ISectorGraphBuilder, SectorGraphBuilder, BranchPolicy,
│   │   │                           #   internals: plan, validator, executor, compiler
│   ├── Config/                     # INotificationSectorConfig, SectorExecution, NotifliwySectorsAttribute
│   ├── Connectors/                 # NotificationConnector
│   ├── Pipes/                      # IInputPipe, IExportPipe, in-memory implementations
│   ├── Contexts/                   # INotificationSector, NotificationSector
│   ├── Conditions/                 # INotificationCondition (When nodes)
│   ├── Mapper/                     # INotificationMapper (Map nodes)
│   ├── Transform/                  # INotificationTransform (Transform nodes)
│   ├── Join/                       # INotificationJoin (Join nodes)
│   ├── Custom/                     # INotificationCustom (Custom nodes)
│   ├── Exporters/                  # INotificationExporter (Export nodes)
│   └── Diagnostic/                 # Activity and Meter support
├── generators/Notifliwy.Generators/  # [NotifliwySectors] source generator (netstandard2.0,
│                                      #   packed into the Notifliwy package analyzers slot)
├── mapping/
│   ├── Notifliwy.Mapping.Mapperly/  # Mapperly adapter package (net8.0)
│   └── Notifliwy.Mapping.Mapster/   # Mapster adapter package (net8.0)
├── providers/Notifliwy.Provider.MassTransit.Kafka/
│   └── Extensions/                  # MassTransit/Kafka integration
└── diagnostic/Notifliwy.OpenTelemetry.Instrumentation/
    └── Extensions/                  # OpenTelemetry integration

samples/
├── inmemory/                       # Config-class sector: Branch + Join, generated registration
└── kafka/                          # Kafka sample: two-branch BestEffort graph (server, sender)

test/
├── Notifliwy.Units/                # Core unit tests
├── Notifliwy.Generators.Tests/     # Source generator tests
├── Notifliwy.Mapping.Tests/        # Mapping adapter tests
└── Notifliwy.Benchmark/            # BenchmarkDotNet (incl. Compiled vs Scoped comparison)
```
