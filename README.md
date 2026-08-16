![banner](https://raw.githubusercontent.com/dot-stbl/notifliwy/refs/heads/master/contents/banner.dither.violet.png)

<p align="center">
  <a href="https://www.nuget.org/packages/Notifliwy"><img alt="Notifliwy on NuGet" src="https://img.shields.io/nuget/v/Notifliwy?style=flat-square&label=NuGet&color=101010" /></a>
  <a href="https://github.com/dot-stbl/notifliwy/blob/master/src/libraries/Notifliwy/Notifliwy.csproj"><img alt="Target frameworks: net6.0, net7.0, net8.0" src="https://img.shields.io/badge/net-6.0%20%C2%B7%207.0%20%C2%B7%208.0-101010?style=flat-square" /></a>
  <a href="https://github.com/dot-stbl/notifliwy/blob/master/LICENSE"><img alt="License: GPL-3.0-or-later" src="https://img.shields.io/badge/License-GPL--3.0--or--later-101010?style=flat-square" /></a>
</p>

<details>
<summary><strong>One core library and four add-ons, all published on NuGet.</strong></summary>

[![Notifliwy](https://img.shields.io/nuget/dt/Notifliwy?style=flat-square&label=Notifliwy&color=101010)](https://www.nuget.org/packages/Notifliwy)
[![Kafka provider](https://img.shields.io/nuget/dt/Notifliwy.Provider.MassTransit.Kafka?style=flat-square&label=Provider.MassTransit.Kafka&color=101010)](https://www.nuget.org/packages/Notifliwy.Provider.MassTransit.Kafka)
[![OpenTelemetry](https://img.shields.io/nuget/dt/Notifliwy.OpenTelemetry.Instrumentation?style=flat-square&label=OpenTelemetry.Instrumentation&color=101010)](https://www.nuget.org/packages/Notifliwy.OpenTelemetry.Instrumentation)
[![Mapperly mapping](https://img.shields.io/nuget/dt/Notifliwy.Mapping.Mapperly?style=flat-square&label=Mapping.Mapperly&color=101010)](https://www.nuget.org/packages/Notifliwy.Mapping.Mapperly)
[![Mapster mapping](https://img.shields.io/nuget/dt/Notifliwy.Mapping.Mapster?style=flat-square&label=Mapping.Mapster&color=101010)](https://www.nuget.org/packages/Notifliwy.Mapping.Mapster)

</details>
<p></p>

Declare how an incoming event becomes something worth sending — as a typed graph: filter it, map it, transform it, branch it to several destinations, join it back — and a hosted service runs that graph for every event that arrives, in its own DI scope, already traced.

```text
→ a graph, not a Consume() method    When → Map → (Transform | Branch | Join | Export)*
→ one event to many sectors          not one consumer per destination
→ config classes + source-gen        not lambda blocks in Program.cs
→ compiled hot path when possible    scoped fallback when not
→ traced from the start              not instrumented afterwards
```

## See it in action

A payment fails three times, and someone has to hear about it. The whole path — the filter that drops the noisy first two attempts, the conversion, the two deliveries — is one configuration class and one registration line.

```csharp
using Notifliwy.Config.Interfaces;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Dependency;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Graph.Interfaces;

builder.Services.AddNotifliwyServer(server =>
{
    server.AddInMemoryInput();
    server.AddSector<PaymentSector>();
});

public record PaymentFailed(string OrderId, string Reason, int Attempt);
public record PaymentAlert(string Text);

public class PaymentSector : INotificationSectorConfig<PaymentAlert, PaymentFailed>
{
    public void Configure(ISectorGraphBuilder<PaymentAlert, PaymentFailed> graph)
    {
        graph
            .When<AfterThirdAttempt>()
            .Map((inputEvent, _) => ValueTask.FromResult(
                new PaymentAlert($"{inputEvent.OrderId} failed {inputEvent.Attempt}x - {inputEvent.Reason}")))
            .Branch(
                branch => branch.Export<EmailExporter>(),
                branch => branch.Export<AuditExporter>());
    }
}

public class AfterThirdAttempt : INotificationCondition<PaymentAlert, PaymentFailed>
{
    public ValueTask<bool> AllowItAsync(PaymentFailed inputEvent, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(inputEvent.Attempt >= 3);
}

public class EmailExporter : INotificationExporter<PaymentAlert>
{
    public ValueTask ThrowAsync(PaymentAlert notification, CancellationToken cancellationToken = default)
        => new(Console.Out.WriteLineAsync($"email: {notification.Text}"));
}

public class AuditExporter : INotificationExporter<PaymentAlert>
{
    public ValueTask ThrowAsync(PaymentAlert notification, CancellationToken cancellationToken = default)
        => new(Console.Out.WriteLineAsync($"audit: {notification.Text}"));
}
```

Anything that can inject `IExportPipe<PaymentFailed>` now feeds the sector:

```csharp
await exportPipe.ExportAsync(new PaymentFailed("A-1029", "card_declined", 3));
// email: A-1029 failed 3x - card_declined
// audit: A-1029 failed 3x - card_declined
```

One-off sector, no class? The same graph inline:

```csharp
server.AddSector<PaymentAlert, PaymentFailed>(graph => graph
    .When<AfterThirdAttempt>()
    .Map<PaymentAlertMapper>()
    .Transform<RetryCountTransform>()
    .Export<EmailExporter>());
```

Several sectors for one event type, or a whole assembly of config classes? Mark the assembly once and call the generated registration — the source generator ships inside the core package:

```csharp
[assembly: NotifliwySectors]

builder.Services.AddNotifliwyServer(server =>
{
    server.AddInMemoryInput();
    server.AddNotifliwySectors(); // generated: one AddSector<TConfig>() per config class, zero reflection
});
```

<details>
<summary><strong>What actually runs for every event</strong></summary>

Nothing above is dispatched by reflection or by a message bus. One `BackgroundService` per event type reads the pipe, and the rest comes from the frozen graph plan:

```text
event
 └─ IInputPipe<TEvent>.AcceptAsync()   one IAsyncEnumerable per event type
    └─ NotificationConnector<TEvent>   BackgroundService, started by the host
       └─ sectors, in parallel         Parallel.ForEachAsync, MaxDegreeOfParallelism = ProcessorCount
          └─ NotificationSector        opens its own async DI scope, catches and logs
             └─ SectorGraphExecutor    conditions → Map → node walk over the graph plan
                ├─ INotificationCondition.AllowItAsync   false on any When -> event is dropped
                ├─ INotificationMapper.ConvertAsync      required, this is where the notification appears
                ├─ INotificationTransform.TransformAsync sequential nodes on the path
                ├─ Branch fan-out                       parallel sub-graphs under a BranchPolicy
                ├─ INotificationJoin.JoinAsync          reduces branch outputs back into the path
                └─ INotificationExporter.ThrowAsync      exports run in order at their path position
```

| Node | Interface | Method | Rules |
|---|---|---|---|
| Input | `IInputPipe<TEvent>` | `AcceptAsync` | `AddInMemoryInput()` or a provider |
| When | `INotificationCondition<TNotification, TEvent>` | `AllowItAsync` | before `Map`, all must allow |
| Map | `INotificationMapper<TNotification, TEvent>` | `ConvertAsync` | exactly once, before every other node |
| Transform | `INotificationTransform<TNotification>` | `TransformAsync` | sequential, each gets the previous output |
| Branch | sub-graphs of the same builder | — | fan-out runs in parallel under a policy |
| Join | `INotificationJoin<TNotification>` | `JoinAsync` | reduces branch outputs; single branch = passthrough |
| Custom | `INotificationCustom<TNotification>` | `InvokeAsync` | escape hatch for anything transform-shaped |
| Export | `INotificationExporter<TNotification>` | `ThrowAsync` | any path position, serial in order |

The graph is acyclic by construction and validated at startup: exactly one `Map` before any node, `When` before `Map`, `Join` only after a `Branch`, and every branch sub-graph ends with at least one `Export`.

**Two execution paths, chosen at startup.** `SectorGraphExecutor` walks the same plan either way:

- **Compiled** — every node resolved or constructed once at startup, direct invokes, no per-event DI scope. A node is compile-safe when it is singleton-registered or stateless. `SectorExecution.Compiled` demands this and fails fast (`SectorCaptiveDependencyException`) on scoped nodes.
- **Scoped** — a fresh DI scope per event, every node resolved from it. The default `SectorExecution.Auto` compiles when it can and falls back here with a logged reason (e.g. an exporter holding a scoped `DbContext`).

A full host with a branched graph, two exporters, a Kafka rider and OpenTelemetry wired up lives in this repository:
[samples/kafka/Notifliwy.Sample.Kafka.Server](https://github.com/dot-stbl/notifliwy/blob/master/samples/kafka/Notifliwy.Sample.Kafka.Server/Program.cs).

</details>

## Why wire it this way

- **One event, many destinations** — register several sectors for the same event type and they run in parallel, each in its own scope; a failing exporter in one sector does not stop the others. Inside one sector, a `Branch` fans the notification out to parallel sub-graphs.
- **The graph is a class** — a sector configuration is a plain `INotificationSectorConfig<TNotification, TEvent>` with constructor dependencies if it needs them, unit-tested without a host, discovered at compile time by the source generator.
- **The same sector on any transport** — moving from an in-process channel to a Kafka topic changes the input pipe registration and nothing else.
- **Traced from the first event** — the connector and every sector open an `Activity` tagged with the event and notification type, and one call hands that source to OpenTelemetry.

## Quick start

**Requires the .NET SDK 8.0 or higher to build. The core library targets net6.0, net7.0 and net8.0** — there is no net9.0 or net10.0 target yet, and .NET 6 and 7 are past Microsoft's support window. The mapping packages target net8.0 only.

```bash
dotnet add package Notifliwy
```

Register the server once, in `Program.cs`, exactly as in the example above. Then publish events from anywhere in the application:

```csharp
public class PaymentGateway(IExportPipe<PaymentFailed> exportPipe)
{
    public ValueTask ReportAsync(string orderId, string reason, int attempt, CancellationToken cancellationToken = default)
        => exportPipe.ExportAsync(new PaymentFailed(orderId, reason, attempt), cancellationToken);
}
```

A runnable version of the graph — fan-out to two branches, a join reducing them, a summary export — is in
[samples/inmemory](https://github.com/dot-stbl/notifliwy/blob/master/samples/inmemory/Notifliwy.Sample.InMemory/Program.cs) — `dotnet run --project samples/inmemory/Notifliwy.Sample.InMemory`.

**Coming from 3.1?** The linear fluent sector API (`AddNotification`, `WithPipeline`, `INotificationStep`) is removed in 3.2, not deprecated. Every removed call has a graph equivalent in the migration guide: [docs/MIGRATION-3.2.md](https://github.com/dot-stbl/notifliwy/blob/master/docs/MIGRATION-3.2.md).

## Packages

| Package | What it adds | Frameworks |
|---|---|---|
| [**Notifliwy**](https://www.nuget.org/packages/Notifliwy) [![Notifliwy version](https://img.shields.io/nuget/v/Notifliwy?style=flat-square&label=&color=101010)](https://www.nuget.org/packages/Notifliwy) | sector graphs, config-class registration, the sector source generator, connector, the in-memory pipe, tracing and metrics | net6.0 · net7.0 · net8.0 |
| [**Notifliwy.Provider.MassTransit.Kafka**](https://www.nuget.org/packages/Notifliwy.Provider.MassTransit.Kafka) [![Kafka provider version](https://img.shields.io/nuget/v/Notifliwy.Provider.MassTransit.Kafka?style=flat-square&label=&color=101010)](https://www.nuget.org/packages/Notifliwy.Provider.MassTransit.Kafka) | turns a MassTransit Kafka rider into an input pipe | net6.0 · net7.0 · net8.0 |
| [**Notifliwy.OpenTelemetry.Instrumentation**](https://www.nuget.org/packages/Notifliwy.OpenTelemetry.Instrumentation) [![OpenTelemetry package version](https://img.shields.io/nuget/v/Notifliwy.OpenTelemetry.Instrumentation?style=flat-square&label=&color=101010)](https://www.nuget.org/packages/Notifliwy.OpenTelemetry.Instrumentation) | one call each for the tracer and the meter provider | net6.0 · net7.0 · net8.0 |
| [**Notifliwy.Mapping.Mapperly**](https://www.nuget.org/packages/Notifliwy.Mapping.Mapperly) [![Mapperly mapping version](https://img.shields.io/nuget/v/Notifliwy.Mapping.Mapperly?style=flat-square&label=&color=101010)](https://www.nuget.org/packages/Notifliwy.Mapping.Mapperly) | plugs a Mapperly source-generated mapper into a `Map` node | net8.0 |
| [**Notifliwy.Mapping.Mapster**](https://www.nuget.org/packages/Notifliwy.Mapping.Mapster) [![Mapster mapping version](https://img.shields.io/nuget/v/Notifliwy.Mapping.Mapster?style=flat-square&label=&color=101010)](https://www.nuget.org/packages/Notifliwy.Mapping.Mapster) | plugs a compiled Mapster mapping into a `Map` node | net8.0 |

<details>
<summary><strong>Compile-time mapping with Mapperly (or runtime mapping with Mapster)</strong></summary>

A `Map` node takes a hand-written `INotificationMapper<TNotification, TEvent>`, an inline lambda, or an adapter from a mapping package. The Mapperly adapter wraps a source-generated mapper — zero reflection at event time:

```csharp
public interface IPaymentMapping : IMapperlyNotificationMapping<PaymentAlert, PaymentFailed>;

[Mapper]
public sealed partial class PaymentMapper : IPaymentMapping
{
    public partial PaymentAlert ToNotification(PaymentFailed inputEvent);
}

// registration
services.AddNotifliwyMapperlyMapping<PaymentAlert, PaymentFailed, PaymentMapper>();

// then in the graph, mapping is compile-time generated
graph.Map(new MapperlyNotificationMapper<PaymentAlert, PaymentFailed, PaymentMapper>(new PaymentMapper()));
```

Mapster brings your existing `TypeAdapterConfig` rules the same way: `new MapsterNotificationMapper<PaymentAlert, PaymentFailed>(config)` compiles the delegate once and each conversion is a plain invocation.

When to use which: a lambda for one-liners, the core `INotificationMapper` class for real logic, a mapping package when the mapping already exists (or should be source-generated) in Mapperly or Mapster shape.

</details>

<details>
<summary><strong>Consuming a Kafka topic instead of the in-memory channel</strong></summary>

`AddNotifliwyPipe<TEvent>()` registers the consumer, `ConfigureNotifliwyPipe(context)` attaches it to a topic endpoint. The sector configuration stays exactly as it is:

```csharp
configurator.AddRider(rider =>
{
    rider.AddNotifliwyPipe<PaymentFailed>();

    rider.UsingKafka((context, kafka) =>
    {
        kafka.Host("localhost:9092");

        kafka.TopicEndpoint<PaymentFailed>(
            groupId: "payments",
            topicName: "payment.failed",
            configure: endpoint => endpoint.ConfigureNotifliwyPipe(context));
    });
});
```

Two delivery modes, and the choice matters:

- **`ConfigureNotifliwyPipe(context)`** — the MassTransit consumer awaits the sectors directly. Kafka does not advance the offset until every sector has finished, so MassTransit's retry and circuit-breaker middleware still covers your pipeline.
- **`ConfigureNotifliwyPipe(context, withConnector: true)`** — the message is written into a bounded in-memory channel (10 000 slots) and picked up by `NotificationConnector`. The consumer returns immediately, which decouples consumption from processing and means a crash loses whatever is still buffered.

Serialization is MassTransit's default for the rider (JSON unless you plug your own factory). Bring any serializer the MassTransit Kafka rider supports — it is not part of Notifliwy.

</details>

<details>
<summary><strong>Traces and metrics</strong></summary>

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddNotifliwyServerInstrumentation())
    .WithMetrics(metrics => metrics.AddMeter("Notifliwy.Server"));
```

Spans: `notifliwy.connector` for each event taken off the pipe, `notifliwy.transaction.sector` for each sector that handles it, tagged with `event.type` and `notification.type`. A sector that throws records the exception on its span and sets the status to `Error`.

Counters, both on the `Notifliwy.Server` meter:

- `notifliwy.server.event.count` — events accepted from the input pipe
- `notifliwy.server.sector.count` — sector passes, counted in a `finally`, so filtered and failed events are in there too

`MeterProviderBuilder.AddNotifliwyServerInstrumentation()` exists, but it currently subscribes by instrument name instead of meter name, so nothing is collected — use `AddMeter("Notifliwy.Server")` until that is fixed. Tracing has no such problem.

The Kafka sample ships a Jaeger container for exactly this:
`docker-compose -f deploy/sample-kafka-compose.yml up -d`, then Jaeger at `localhost:16686`.

</details>

## Docs

**Start here:** [docs/NOTIFLIWY.md](https://github.com/dot-stbl/notifliwy/blob/master/docs/NOTIFLIWY.md) — graph reference, then the samples for a host you can run.

→ **[Migration guide](https://github.com/dot-stbl/notifliwy/blob/master/docs/MIGRATION-3.2.md)**: every removed 3.1 call mapped to its 3.2 graph equivalent<br>
→ **[Architecture reference](https://github.com/dot-stbl/notifliwy/blob/master/docs/NOTIFLIWY.md)**: graph nodes, execution modes, fan-out and join patterns, key files<br>
→ **[Known issues](https://github.com/dot-stbl/notifliwy/blob/master/docs/BUGS.md)**: what is fixed, what is deliberate, what is still open<br>
→ **[Coding rules](https://github.com/dot-stbl/notifliwy/tree/master/.claude/rules)**: the C# conventions a PR is reviewed against<br>
→ **[Samples](https://github.com/dot-stbl/notifliwy/tree/master/samples)**: an in-memory host with branch + join, and a Kafka host with a two-branch BestEffort graph

## How it compares

**vs. [MediatR](https://github.com/jbogard/MediatR) notifications** — the shortest way to fan an in-process event out to several handlers, and hard to beat inside a single call stack. Notifliwy starts at the other end: a stream a background service keeps consuming, one DI scope per event, and the handler split into filter, map, enrich, branch and deliver so each part stands on its own.

**vs. plain [MassTransit](https://masstransit.io) consumers** — a mature transport layer with retries, circuit breakers and an outbox, and Notifliwy does not replace any of it. It sits on the same Kafka rider and takes over what usually accumulates inside `Consume()`: filtering, conversion, and delivery to more than one destination.

**vs. a hand-rolled `BackgroundService`** — a `Channel<T>` and a `while` loop is thirty lines and works. It stops working when the second destination appears, then the third, and the loop becomes a switch over event types with no per-event scope and no spans.

## Usage notes

**Failures are logged, not retried.** An exception anywhere inside a sector — condition, mapper, transform, exporter — is caught in `NotificationSector`, recorded on the span and written to the log; the event is then dropped. Inside a `Branch` fan-out the `BranchPolicy` decides: `FailFast` (default) rethrows the first fault after all branches are observed, `BestEffort` logs and skips failed branches so the survivors — and the following `Join` — continue. There is no retry and no dead-letter path. Put retries inside the exporter, or consume through MassTransit and let its middleware own them.

**A sector with no `When` conditions allows everything.** The graph walks the condition set before anything else runs; an empty set means "allow" and processing continues straight to `Map`.

**Branches share one notification instance and one DI scope.** Every branch of a fan-out receives the same notification object that entered the `Branch` node and resolves its nodes from the same per-event scope. Transforms and custom nodes should return new instances instead of mutating; a node with real scoped state (e.g. a `DbContext`) is not branch-isolated. The main path between a `Branch` and a `Join` keeps the pre-branch notification — without a `Join`, downstream nodes continue with what the fan-out received.

**The in-memory pipe is process-local and sized through options.** `AddInMemoryInput()` creates a bounded `Channel<TEvent>` of 1 000 000 items in `Wait` mode: producers block when it fills up, and everything queued is gone on restart. The `AddInMemoryInput(configure)` overload is the real knob — the callback receives `InMemoryExchangeOptions`, and its `ChannelOptions` (a `BoundedChannelOptions` or `UnboundedChannelOptions`) is what the exchange builds its channel from:

```csharp
server.AddInMemoryInput(options => options.ChannelOptions = new BoundedChannelOptions(10_000)
{
    FullMode = BoundedChannelFullMode.DropOldest
});
```

**Testing a sector on a bare `ServiceCollection`?** Call `AddLogging()` first — the sector resolves `ILogger<T>`, which a generic host registers for you and a bare collection does not. `AddInMemoryInput` registers the options infrastructure itself, so `AddOptions()` is no longer required just to activate the pipe.

**Registering a whole assembly of sectors.** `[assembly: NotifliwySectors]` + the generated `AddNotifliwySectors()` is the primary path — compile-time discovery, public and internal config classes, zero reflection. The opt-in fallback `AddSectorsFromAssembly(assembly)` discovers public config classes at runtime and logs a startup warning recommending the generator.

## Contributing

**Small fixes** — bug fixes and typos can go straight to a pull request.

**Larger changes** — open an issue first so the shape of the API is agreed before it is written; this library's whole value is its surface.

**AI-generated code is welcome** — as long as it is tested and reviewed by you. Name the agent and the model in the pull request.

### Development

- Build: `dotnet build notifliwy.sln`
- Test: `dotnet test test/Notifliwy.Units` (core), `test/Notifliwy.Generators.Tests` (source generator), `test/Notifliwy.Mapping.Tests` (mapping adapters)
- Coverage: `dotnet test test/Notifliwy.Units --collect:"XPlat Code Coverage"`
- One test: `dotnet test test/Notifliwy.Units --filter "FullyQualifiedName~EndToEnd"`
- Benchmarks: `dotnet run --project test/Notifliwy.Benchmark` — includes the `CompiledVsScopedBenchmarks` execution-path comparison (run a full BDN session from a Release build of `src/` so the package-on-build targets find their artifacts)
- In-memory sample: `dotnet run --project samples/inmemory/Notifliwy.Sample.InMemory`
- Kafka sample: `docker-compose -f deploy/sample-kafka-compose.yml up -d`, then run `Notifliwy.Sample.Kafka.Server` and `Notifliwy.Sample.Kafka.Sender`
- Conventions live in [`.claude/rules/`](https://github.com/dot-stbl/notifliwy/tree/master/.claude/rules) — primary constructors, no private methods, `ValueTask` throughout, structured logging.

There is no CI in this repository yet, so run the tests locally before opening a pull request.

## License

GPL-3.0-or-later. The full text is in [LICENSE](https://github.com/dot-stbl/notifliwy/blob/master/LICENSE), and every package published from this repository carries the same expression. This is a copyleft license rather than MIT or Apache-2.0 — worth checking against your own distribution terms before you take a dependency on it.

Built by [`.stbl`](https://github.com/dot-stbl).
