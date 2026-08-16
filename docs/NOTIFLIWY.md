# Notifliwy — Architecture Reference

Notifliwy converts incoming events into notifications through a **typed sector graph**:
`When → Map → (Transform | Branch | Join | Custom | Export)*`, described by sector
configuration classes and executed by a hosted connector. This is the 3.2 surface;
for the removed 3.1 fluent API and every mapping from it, see
[MIGRATION-3.2.md](MIGRATION-3.2.md). For the tour and package table, see the
[README](../README.md).

---

## Pipeline Flow

```
Event → InputPipe → Connector → Sector → SectorGraphExecutor
                                        ├─ When conditions (all must allow)
                                        ├─ Map (exactly one)
                                        └─ node walk: Transform | Branch ⇉ Join | Custom | Export
```

## Graph Nodes

| Node | Interface | Method | Rules |
|------|-----------|--------|-------|
| **Input** | `IInputPipe<TEvent>` | `AcceptAsync()` | transport; `AddInMemoryInput()` or a provider |
| **When** | `INotificationCondition<TN, TE>` | `AllowItAsync(event)` | optional filter on the raw event; before `Map`; all must allow |
| **Map** | `INotificationMapper<TN, TE>` | `ConvertAsync(event)` | **required**, exactly once, before every other node |
| **Transform** | `INotificationTransform<TN>` | `TransformAsync(notif)` | sequential on the path, each receives the previous output |
| **Branch** | sub-graphs of the same builder | — | parallel fan-out under a `BranchPolicy` |
| **Join** | `INotificationJoin<TN>` | `JoinAsync(IReadOnlyList<TN>)` | reduces branch outputs; single-branch join is a passthrough |
| **Custom** | `INotificationCustom<TN>` | `InvokeAsync(notif)` | escape hatch for transform-shaped behaviour |
| **Export** | `INotificationExporter<TN>` | `ThrowAsync(notif)` | delivery at the current path position; serial in order |

`Map` and `Custom` also accept inline lambdas. The graph is acyclic by construction;
the frozen plan is validated when built: exactly one `Map`, `When` before `Map`,
`Join` only after a `Branch`, and every branch sub-graph terminates with at least
one `Export`.

## Registration Surfaces

**Config class (primary):**

```csharp
public class PaymentSector : INotificationSectorConfig<PaymentAlert, PaymentFailed>
{
    public void Configure(ISectorGraphBuilder<PaymentAlert, PaymentFailed> graph)
    {
        graph
            .When<AfterThirdAttempt>()
            .Map<PaymentAlertMapper>()
            .Transform<EnrichmentTransform>()
            .Export<EmailExporter>();
    }
}

serverBuilder.AddSector<PaymentSector>();
```

Config classes are transient DI services — constructor dependencies (options,
clients) are injected when the graph is materialized. Sector-level options:
`SectorExecution Execution` (default `Auto`) and `BranchPolicy? DefaultBranchPolicy`
(default `null` → `FailFast`).

**Inline one-off:** `serverBuilder.AddSector<TNotification, TEvent>(graph => …)` —
built and validated at registration time.

**Assembly discovery:** `[assembly: NotifliwySectors]` marks an assembly; the source
generator (shipped inside the `Notifliwy` package) emits
`Notifliwy.Generated.NotifliwySectorsRegistration.AddNotifliwySectors(serverBuilder)`
with one direct `AddSector<TConfig>()` per concrete, closed, visible config class.
The opt-in runtime fallback `AddSectorsFromAssembly(assembly)` discovers public
config classes by reflection and logs a startup warning.

## Execution Modes

`SectorGraphExecutor` (internal) walks the same plan on one of two paths, selected
at startup:

- **Compiled** — every node resolved or constructed once at startup, direct
  invokes, no per-event DI scope. A node is *compile-safe* when it is
  singleton-registered, transient with singleton-safe dependencies, or stateless
  with a public parameterless constructor.
- **Scoped** — fresh DI scope per event; every node resolved from it.

`SectorExecution`:

| Mode | Behaviour |
|------|-----------|
| `Auto` (default) | compiled when every node is compile-safe; otherwise scoped with a logged reason |
| `Compiled` | forces the compiled path; fails fast at startup with `SectorCaptiveDependencyException` on scoped/unprovable nodes |
| `Scoped` | always per-event scope |

## Branch and Join Semantics

- `Branch(policy?, params branches)` fans the **same notification instance** out to
  parallel sub-graphs that share the per-event DI scope. Transforms and custom nodes
  must **return new instances instead of mutating** — mutation leaks across branches.
- `BranchPolicy.FailFast` (default) — the first fault rethrows after all branches are
  observed. `BranchPolicy.BestEffort` — failed branches are logged and skipped; a
  following `Join` receives only the survivors.
- The main-path notification between a `Branch` and a `Join` stays the **pre-branch
  input**; without a `Join`, downstream nodes continue with what the fan-out received.
- `Join<TJoin>()` reduces branch outputs in registration order; a single surviving
  branch is a passthrough (the reducer is not invoked).

## Mapping Providers

The core is mapper-agnostic; two adapter packages plug external mappers into a `Map`
node:

| Package | Shape |
|---------|-------|
| `Notifliwy.Mapping.Mapperly` | `IMapperlyNotificationMapping<TN, TE>` bridge contract implemented by a `[Mapper]` partial class; `MapperlyNotificationMapper<TN, TE, TMapper>` adapts it; `AddNotifliwyMapperlyMapping<TN, TE, TMapper>()` registers it |
| `Notifliwy.Mapping.Mapster` | `MapsterNotificationMapper<TN, TE>(TypeAdapterConfig)` compiles the Mapster delegate once; `AddNotifliwyMapsterMapping(...)` registers it |

When to use which: an inline lambda for one-liners, a hand-written
`INotificationMapper` class for real logic, an adapter package when the mapping
already exists (or should be source-generated) in Mapperly or Mapster shape.

## Processing Model

- **Sectors process events in parallel** — `Parallel.ForEachAsync`,
  `MaxDegreeOfParallelism = ProcessorCount`; each sector in its own DI scope
- **Nodes on a path run sequentially** in registration order; branch sub-graphs run
  in parallel
- Failures inside a sector are caught in `NotificationSector`, recorded on the span,
  logged, and the event is dropped (no retry / dead-letter; branch failures follow
  the `BranchPolicy`)

## Observability

Spans (`ActivitySource`): `notifliwy.connector` per event taken off the pipe,
`notifliwy.transaction.sector` per sector pass, tagged with `event.type` and
`notification.type`. Metrics (`Notifliwy.Server` meter):
`notifliwy.server.event.count`, `notifliwy.server.sector.count`.

Wire-up:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddNotifliwyServerInstrumentation())
    .WithMetrics(metrics => metrics.AddMeter("Notifliwy.Server"));
```

## Known Issues

See [BUGS.md](BUGS.md) — **no open items**; everything recorded there is fixed or
documented as intentional design.

## Key Files Reference

| File | Purpose |
|------|---------|
| `src/libraries/Notifliwy/Builders/NotificationServerBuilder.cs` | `AddSector` / `AddSectorsFromAssembly` / `AddInMemoryInput` |
| `src/libraries/Notifliwy/Graph/Interfaces/ISectorGraphBuilder.cs` | Graph builder contract (When/Map/Transform/Branch/Join/Custom/Export) |
| `src/libraries/Notifliwy/Graph/Internals/SectorGraphExecutor.cs` | Plan execution: compiled + scoped paths, branch policies |
| `src/libraries/Notifliwy/Graph/Internals/SectorGraphCompiler.cs` | Compiled-path selection, compile-safe node analysis |
| `src/libraries/Notifliwy/Graph/Internals/SectorGraphValidator.cs` | Startup graph validation |
| `src/libraries/Notifliwy/Config/Interfaces/INotificationSectorConfig.cs` | Config-class contract, `Execution`, `DefaultBranchPolicy` |
| `src/libraries/Notifliwy/Config/NotifliwySectorsAttribute.cs` | `[assembly: NotifliwySectors]` marker |
| `src/generators/Notifliwy.Generators/NotifliwySectorsRegistrationGenerator.cs` | Source generator emitting `AddNotifliwySectors()` |
| `src/libraries/Notifliwy/Connectors/NotificationConnector.cs` | Background event processor |
| `src/libraries/Notifliwy/Contexts/NotificationSector.cs` | Per-event scope, error handling, tracing |
| `src/libraries/Notifliwy/Pipes/InMemory/*` | Channel-based in-memory transport |
| `src/mapping/Notifliwy.Mapping.Mapperly/*` | Mapperly adapter package |
| `src/mapping/Notifliwy.Mapping.Mapster/*` | Mapster adapter package |
| `src/libraries/Notifliwy/Diagnostic/DiagnosticActivity.cs` | Activity source for tracing |
| `src/libraries/Notifliwy/Diagnostic/DiagnosticMeter.cs` | Metrics meter |

## Commands

```bash
# Build the solution
dotnet build notifliwy.sln

# Run core unit tests
dotnet test test/Notifliwy.Units

# Source generator + mapping adapter tests
dotnet test test/Notifliwy.Generators.Tests
dotnet test test/Notifliwy.Mapping.Tests

# Run benchmarks (see CLAUDE.md for the Release-artifacts note)
dotnet run --project test/Notifliwy.Benchmark

# Run in-memory sample (branch + join graph, generated registration)
dotnet run --project samples/inmemory/Notifliwy.Sample.InMemory

# Run Kafka sample (two-branch BestEffort graph)
docker-compose -f deploy/sample-kafka-compose.yml up -d
dotnet run --project samples/kafka/Notifliwy.Sample.Kafka.Server
dotnet run --project samples/kafka/Notifliwy.Sample.Kafka.Sender
```

## Development Rules

Coding standards are defined in [`.claude/rules/`](../.claude/rules/):
[naming](../.claude/rules/naming.md), [patterns](../.claude/rules/patterns.md),
[xml-docs](../.claude/rules/xml-docs.md), [logging](../.claude/rules/logging.md),
[result](../.claude/rules/result.md).
