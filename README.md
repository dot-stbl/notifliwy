![banner](https://raw.githubusercontent.com/dot-stbl/notifliwy/refs/heads/master/contents/banner.dither.violet.png)

<p align="center">
  <a href="https://www.nuget.org/packages/Notifliwy"><img alt="Notifliwy on NuGet" src="https://img.shields.io/nuget/v/Notifliwy?style=flat-square&label=NuGet&color=101010" /></a>
  <a href="https://github.com/dot-stbl/notifliwy/blob/master/src/libraries/Notifliwy/Notifliwy.csproj"><img alt="Target frameworks: net6.0, net7.0, net8.0" src="https://img.shields.io/badge/net-6.0%20%C2%B7%207.0%20%C2%B7%208.0-101010?style=flat-square" /></a>
  <a href="https://github.com/dot-stbl/notifliwy/blob/master/LICENSE"><img alt="License: GPL-3.0-or-later" src="https://img.shields.io/badge/License-GPL--3.0--or--later-101010?style=flat-square" /></a>
</p>

<details>
<summary><strong>One core library and two add-ons, all published on NuGet.</strong></summary>

[![Notifliwy](https://img.shields.io/nuget/dt/Notifliwy?style=flat-square&label=Notifliwy&color=101010)](https://www.nuget.org/packages/Notifliwy)
[![Kafka provider](https://img.shields.io/nuget/dt/Notifliwy.Provider.MassTransit.Kafka?style=flat-square&label=Provider.MassTransit.Kafka&color=101010)](https://www.nuget.org/packages/Notifliwy.Provider.MassTransit.Kafka)
[![OpenTelemetry](https://img.shields.io/nuget/dt/Notifliwy.OpenTelemetry.Instrumentation?style=flat-square&label=OpenTelemetry.Instrumentation&color=101010)](https://www.nuget.org/packages/Notifliwy.OpenTelemetry.Instrumentation)

</details>
<p></p>

Declare how an incoming event becomes something worth sending — filter it, map it, enrich it, deliver it — and a hosted service runs that path for every event that arrives, in its own DI scope, already traced.

```text
→ four small stages           not one Consume() method
→ one event to many sectors   not one consumer per destination
→ stages resolved from DI     not constructed by the transport
→ traced from the start       not instrumented afterwards
```

## See it in action

A payment fails three times, and someone has to hear about it. The whole path — the filter that drops the noisy first two attempts, the conversion, the delivery — is four classes and one registration block.

```csharp
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Dependency;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Mapper.Interfaces;

builder.Services.AddNotifliwyServer(server =>
{
    server.AddInMemoryInput();

    server.AddNotification<PaymentAlert, PaymentFailed>(sector =>
    {
        sector.AddCondition<AfterThirdAttempt>();
        sector.AddMapper<PaymentAlertMapper>();
        sector.AddExporter<ConsoleExporter>();
    });
});

public record PaymentFailed(string OrderId, string Reason, int Attempt);
public record PaymentAlert(string Text);

public class AfterThirdAttempt : INotificationCondition<PaymentAlert, PaymentFailed>
{
    public ValueTask<bool> AllowItAsync(PaymentFailed inputEvent, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(inputEvent.Attempt >= 3);
}

public class PaymentAlertMapper : INotificationMapper<PaymentAlert, PaymentFailed>
{
    public ValueTask<PaymentAlert> ConvertAsync(PaymentFailed inputEvent, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new PaymentAlert($"{inputEvent.OrderId} failed {inputEvent.Attempt}x - {inputEvent.Reason}"));
}

public class ConsoleExporter : INotificationExporter<PaymentAlert>
{
    public ValueTask ThrowAsync(PaymentAlert notification, CancellationToken cancellationToken = default)
        => new(Console.Out.WriteLineAsync(notification.Text));
}
```

Anything that can inject `IExportPipe<PaymentFailed>` now feeds the sector:

```csharp
await exportPipe.ExportAsync(new PaymentFailed("A-1029", "card_declined", 3));
// A-1029 failed 3x - card_declined
```

<details>
<summary><strong>What actually runs for every event</strong></summary>

Nothing above is dispatched by reflection or by a message bus. One `BackgroundService` per event type reads the pipe, and the rest is resolved from the container:

```text
event
 └─ IInputPipe<TEvent>.AcceptAsync()   one IAsyncEnumerable per event type
    └─ NotificationConnector<TEvent>   BackgroundService, started by the host
       └─ sectors, in parallel         Parallel.ForEachAsync, MaxDegreeOfParallelism = ProcessorCount
          └─ NotificationSector        opens its own async DI scope, catches and logs
             └─ SectorBlock
                ├─ INotificationCondition.AllowItAsync   false on any condition -> event is dropped
                ├─ INotificationMapper.ConvertAsync      required, this is where the notification appears
                ├─ INotificationStep.AggregateAsync      steps run in order, pipelines chain into each other
                └─ INotificationExporter.ThrowAsync      every registered exporter gets the same notification
```

| Stage | Interface | Method | Required |
|---|---|---|---|
| Input | `IInputPipe<TEvent>` | `AcceptAsync` | yes — `AddInMemoryInput()` or a provider |
| Condition | `INotificationCondition<TNotification, TEvent>` | `AllowItAsync` | see the note in [Usage notes](#usage-notes) |
| Mapper | `INotificationMapper<TNotification, TEvent>` | `ConvertAsync` | yes |
| Step | `INotificationStep<TNotification>` | `AggregateAsync` | no |
| Exporter | `INotificationExporter<TNotification>` | `ThrowAsync` | no |

A full host with two pipelines, two exporters, a Kafka rider and OpenTelemetry wired up lives in this repository:
[samples/kafka/Notifliwy.Sample.Kafka.Server](https://github.com/dot-stbl/notifliwy/blob/master/samples/kafka/Notifliwy.Sample.Kafka.Server/Program.cs).

</details>

## Why wire it this way

- **One event, many destinations** — register several sectors for the same event type and they run in parallel, each in its own scope; a failing exporter in one sector does not stop the others.
- **Stages are plain classes** — a condition or a mapper is a DI service with a single method, so it is unit-tested without a host, a broker or a topic.
- **The same sector on any transport** — moving from an in-process channel to a Kafka topic changes the input pipe registration and nothing else.
- **Traced from the first event** — the connector and every sector open an `Activity` tagged with the event and notification type, and one call hands that source to OpenTelemetry.

## Quick start

**Requires the .NET SDK 8.0 or higher to build. The library itself targets net6.0, net7.0 and net8.0** — there is no net9.0 or net10.0 target yet, and .NET 6 and 7 are past Microsoft's support window.

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

A runnable version of this, with a background producer, is in
[samples/inmemory](https://github.com/dot-stbl/notifliwy/blob/master/samples/inmemory/Notifliwy.Sample.InMemory/Program.cs) — `dotnet run --project samples/inmemory/Notifliwy.Sample.InMemory`.

## Packages

| Package | What it adds | Frameworks |
|---|---|---|
| [**Notifliwy**](https://www.nuget.org/packages/Notifliwy) [![Notifliwy version](https://img.shields.io/nuget/v/Notifliwy?style=flat-square&label=&color=101010)](https://www.nuget.org/packages/Notifliwy) | builders, connector, sectors, the in-memory pipe, tracing and metrics | net6.0 · net7.0 · net8.0 |
| [**Notifliwy.Provider.MassTransit.Kafka**](https://www.nuget.org/packages/Notifliwy.Provider.MassTransit.Kafka) [![Kafka provider version](https://img.shields.io/nuget/v/Notifliwy.Provider.MassTransit.Kafka?style=flat-square&label=&color=101010)](https://www.nuget.org/packages/Notifliwy.Provider.MassTransit.Kafka) | turns a MassTransit Kafka rider into an input pipe | net6.0 · net7.0 · net8.0 |
| [**Notifliwy.OpenTelemetry.Instrumentation**](https://www.nuget.org/packages/Notifliwy.OpenTelemetry.Instrumentation) [![OpenTelemetry package version](https://img.shields.io/nuget/v/Notifliwy.OpenTelemetry.Instrumentation?style=flat-square&label=&color=101010)](https://www.nuget.org/packages/Notifliwy.OpenTelemetry.Instrumentation) | one call each for the tracer and the meter provider | net6.0 · net7.0 · net8.0 |

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

**Start here:** [docs/NOTIFLIWY.md](https://github.com/dot-stbl/notifliwy/blob/master/docs/NOTIFLIWY.md) — stage-by-stage reference, then the samples for a host you can run.

→ **[Architecture reference](https://github.com/dot-stbl/notifliwy/blob/master/docs/NOTIFLIWY.md)**: every stage, fan-out and multi-sector patterns, key files<br>
→ **[Known issues](https://github.com/dot-stbl/notifliwy/blob/master/docs/BUGS.md)**: what is fixed, what is deliberate, what is still open<br>
→ **[Coding rules](https://github.com/dot-stbl/notifliwy/tree/master/.claude/rules)**: the C# conventions a PR is reviewed against<br>
→ **[Samples](https://github.com/dot-stbl/notifliwy/tree/master/samples)**: an in-memory host and a Kafka host with two pipelines and two exporters

## How it compares

**vs. [MediatR](https://github.com/jbogard/MediatR) notifications** — the shortest way to fan an in-process event out to several handlers, and hard to beat inside a single call stack. Notifliwy starts at the other end: a stream a background service keeps consuming, one DI scope per event, and the handler split into filter, map, enrich and deliver so each part stands on its own.

**vs. plain [MassTransit](https://masstransit.io) consumers** — a mature transport layer with retries, circuit breakers and an outbox, and Notifliwy does not replace any of it. It sits on the same Kafka rider and takes over what usually accumulates inside `Consume()`: filtering, conversion, and delivery to more than one destination.

**vs. a hand-rolled `BackgroundService`** — a `Channel<T>` and a `while` loop is thirty lines and works. It stops working when the second destination appears, then the third, and the loop becomes a switch over event types with no per-event scope and no spans.

## Usage notes

**Failures are logged, not retried.** An exception anywhere inside a sector — condition, mapper, step or exporter — is caught in `NotificationSector`, recorded on the span and written to the log; the event is then dropped. There is no retry and no dead-letter path. Put retries inside the exporter, or consume through MassTransit and let its middleware own them.

**A sector with no conditions allows everything.** `SectorBlock` asks the condition set for a verdict before anything else runs; an empty set means "allow" and processing continues straight to the mapper. A condition is still useful as a cheap filter, and `ValueTask.FromResult(true)` is the no-op version.

**The in-memory pipe is process-local and sized through options.** `AddInMemoryInput()` creates a bounded `Channel<TEvent>` of 1 000 000 items in `Wait` mode: producers block when it fills up, and everything queued is gone on restart. The `AddInMemoryInput(configure)` overload is the real knob — the callback receives `InMemoryExchangeOptions`, and its `ChannelOptions` (a `BoundedChannelOptions` or `UnboundedChannelOptions`) is what the exchange builds its channel from:

```csharp
server.AddInMemoryInput(options => options.ChannelOptions = new BoundedChannelOptions(10_000)
{
    FullMode = BoundedChannelFullMode.DropOldest
});
```

**Testing a sector on a bare `ServiceCollection`?** Call `AddLogging()` first — the sector resolves `ILogger<T>`, which a generic host registers for you and a bare collection does not. `AddInMemoryInput` registers the options infrastructure itself, so `AddOptions()` is no longer required just to activate the pipe.

**Two `WithPipeline` blocks chain, they do not fork.** The notification returned by the first pipeline is what the second one receives, so a later step can overwrite an earlier one. Use one pipeline unless you actually want that ordering.

## Contributing

**Small fixes** — bug fixes and typos can go straight to a pull request.

**Larger changes** — open an issue first so the shape of the API is agreed before it is written; this library's whole value is its surface.

**AI-generated code is welcome** — as long as it is tested and reviewed by you. Name the agent and the model in the pull request.

### Development

- Build: `dotnet build notifliwy.sln`
- Test: `dotnet test test/Notifliwy.Units`
- Coverage: `dotnet test test/Notifliwy.Units --collect:"XPlat Code Coverage"`
- One test: `dotnet test test/Notifliwy.Units --filter "FullyQualifiedName~EndToEnd"`
- Kafka sample: `docker-compose -f deploy/sample-kafka-compose.yml up -d`, then run `Notifliwy.Sample.Kafka.Server` and `Notifliwy.Sample.Kafka.Sender`
- Conventions live in [`.claude/rules/`](https://github.com/dot-stbl/notifliwy/tree/master/.claude/rules) — primary constructors, no private methods, `ValueTask` throughout, structured logging.

There is no CI in this repository yet, so run the tests locally before opening a pull request.

## License

GPL-3.0-or-later. The full text is in [LICENSE](https://github.com/dot-stbl/notifliwy/blob/master/LICENSE), and every package published from this repository carries the same expression. This is a copyleft license rather than MIT or Apache-2.0 — worth checking against your own distribution terms before you take a dependency on it.

Built by [`.stbl`](https://github.com/dot-stbl).
