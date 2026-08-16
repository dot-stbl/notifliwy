# Migrating to Notifliwy 3.2

Notifliwy 3.2 replaces the 3.1 linear fluent sector API with a typed **sector graph**
(`When → Map → (Transform | Branch | Join | Custom | Export)*`), a **config-class
registration** surface, and explicit branch/join execution semantics. This is a hard
breaking change (V3): the 3.1 surface is removed, not deprecated.

Ships with: `INotificationStep` → `INotificationTransform` rename (`AggregateAsync` →
`TransformAsync`), `BranchPolicy`, `INotificationJoin`, `INotificationCustom`,
`SectorExecution`.

## TL;DR mapping table

| 3.1 API | Status | 3.2 graph equivalent |
|---|---|---|
| `serverBuilder.AddNotification<TNotification, TEvent>(sector => { ... })` | **Removed** | `serverBuilder.AddSector<TNotification, TEvent>(graph => ...)` (inline) or `serverBuilder.AddSector<MySectorConfig>()` (config class) |
| `sector.AddCondition<TCondition>()` | **Removed** | `graph.When<TCondition>()` — before `Map`, all conditions must allow |
| `sector.AddMapper<TMapper>()` | **Removed** | `graph.Map<TMapper>()` or `graph.Map((e, ct) => ...)` — exactly once, before all other nodes |
| `sector.WithPipeline(p => p.AddStep<TTransform>())` | **Removed** | `graph.Transform<TTransform>()` — sequential transforms on the main path |
| Multiple `WithPipeline(...)` blocks | **Removed** | Nodes on one graph path chain in registration order; fan-out is now explicit `Branch` (the ambiguous multi-pipeline chaining of GH #9 is closed by design) |
| `sector.AddExporter<TExporter>()` | **Removed** | `graph.Export<TExporter>()` — at any path position; multiple exports run serially in order |
| `NotificationSectorBuilder<TNotification, TEvent>` | **Removed** | `SectorGraphBuilder<TNotification, TEvent>` / `ISectorGraphBuilder<TNotification, TEvent>` |
| `INotificationPipeline<TNotification>` + `NotificationPipeline<T>` | **Removed** | The graph plan itself — sequential `Transform` nodes |
| `PipelineBuilder<TNotification>.AddStep<T>()` | **Removed** | `graph.Transform<T>()` |
| `INotificationStep<T>.AggregateAsync` | **Removed** (renamed) | `INotificationTransform<T>.TransformAsync` |
| `SectorBlock<TNotification, TEvent>` | **Removed** (internal engine) | `SectorGraphExecutor<TNotification, TEvent>` (internal) driven by the frozen plan |
| `INotificationConditionProcessor<TNotification, TEvent>` | **Removed** | Inline condition walk inside the executor |
| `MultiplyServiceInstance<T>` (+ `EmptyInstanceBranchException`, `ToMultiplyService`) | **Removed** | Not needed: graph nodes resolve per-event from DI |
| `INotificationSector<TEvent>`, `NotificationConnector<TEvent>`, in-memory/kafka pipes, `AddNotifliwyServer`, `AddInMemoryInput` | **Unchanged** | — |

No 3.2 equivalent yet (later waves): assembly scanning (`AddSectorsFromAssembly`,
`[NotifliwySectors]` source-gen), compiled execution (`SectorExecution.Compiled`
currently falls back to scoped), Mapperly/Mapster mapping adapters.

## Basic migration

```csharp
// 3.1
serverBuilder.AddNotification<OrderNotification, OrderCreated>(sector =>
{
    sector.AddCondition<LargeOrderCondition>();
    sector.AddMapper<OrderMapper>();
    sector.WithPipeline(p => p
        .AddStep<EnrichmentStep>()
        .AddStep<FormattingStep>());
    sector.AddExporter<EmailExporter>();
    sector.AddExporter<AuditExporter>();
});

// 3.2 — inline (samples, one-offs)
serverBuilder.AddSector<OrderNotification, OrderCreated>(graph => graph
    .When<LargeOrderCondition>()
    .Map<OrderMapper>()
    .Transform<EnrichmentTransform>()
    .Transform<FormattingTransform>()
    .Export<EmailExporter>()
    .Export<AuditExporter>());

// 3.2 — config class (hosts, testable, constructor dependencies from DI)
public class OrderSector : INotificationSectorConfig<OrderNotification, OrderCreated>
{
    public void Configure(ISectorGraphBuilder<OrderNotification, OrderCreated> graph)
    {
        graph
            .When<LargeOrderCondition>()
            .Map<OrderMapper>()
            .Transform<EnrichmentTransform>()
            .Export<EmailExporter>()
            .Export<AuditExporter>();
    }
}

serverBuilder.AddSector<OrderSector>();
```

`INotificationSectorConfig<TNotification, TEvent>` also exposes:

- `SectorExecution Execution` — `Auto` (default) / `Scoped` / `Compiled`
  (`Compiled` is reserved and currently falls back to `Scoped` until compiler
  support lands).
- `BranchPolicy? DefaultBranchPolicy` — sector-level default for every `Branch`
  that does not set its own policy; `null` means `FailFast`.

Config classes are registered in DI as transient services, so they may take
constructor dependencies (options, clients) used while building the graph.

## Pipeline chaining → explicit Branch/Join

3.1 chained every `WithPipeline` block output into the next one — semantics were
ambiguous (GH #9). 3.2 forces the intent:

- Sequential transforms: chain `Transform` nodes — same behaviour as before.
- Independent parallel deliveries: use `Branch(...)` fan-out.
- Fan-out then merge back: `Branch(...)` followed by `Join<TJoin>()`, where
  `TJoin : INotificationJoin<TNotification>` reduces the branch outputs.
  A single-branch `Join` is a passthrough (the reducer is not invoked).

```csharp
graph
    .Map<OrderMapper>()
    .Branch(
        branch => branch.Transform<EmailBodyTransform>().Export<EmailExporter>(),
        branch => branch.Transform<SlackBodyTransform>().Export<SlackExporter>())
    .Join<NotificationMergeJoin>()
    .Export<AuditExporter>();
```

## Behavioural hazards to check during migration

1. **Fan-out shares one notification instance.** Every branch receives the *same*
   notification object that entered the `Branch` node. Transforms and custom nodes
   must **return new instances instead of mutating** the input — a mutating
   transform causes cross-branch interference.
2. **Branches share the per-event DI scope.** All branches of one fan-out resolve
   their nodes from the same scope created for the event. A stateful scoped node
   (e.g. a DbContext-based exporter) is *not* branch-isolated; branches run in
   parallel and would race on shared scoped state. Register such nodes as
   transient-safe or keep their state local.
3. **The main-path notification between `Branch` and `Join` stays the pre-branch
   input.** The fan-out does not change the main-path notification until a `Join`
   consumes the branch outputs. Without a `Join`, downstream nodes continue with
   the notification as it was before the `Branch`.
4. **Branch failures follow a policy.** Default is `BranchPolicy.FailFast`
   (the first fault rethrows after all branches are observed). `BestEffort` logs
   and skips failed branches; a following `Join` receives only the survivors.
   Set per-branch (`Branch(BranchPolicy.BestEffort, ...)`) or per-sector
   (`DefaultBranchPolicy` on the config class).
5. **Graph validation happens at startup** for inline registrations (registration
   time) and at sector materialization for config classes (connector startup in a
   hosted app): exactly one `Map` before any node, `When` before `Map`, `Join`
   only after a `Branch`, every branch sub-graph ends with at least one `Export`.
6. **Multiple exporters are serial**, as in 3.1 — `Export` nodes run in order on
   the same path.

## Renames

| 3.1 | 3.2 |
|---|---|
| `INotificationStep<TNotification>` | `INotificationTransform<TNotification>` |
| `AggregateAsync(notification, ct)` | `TransformAsync(notification, ct)` |
| `WithPipeline(p => p.AddStep<T>())` | `Transform<T>()` |
| `IStagesBuilder` / `PipelineBuilder<T>` | graph plan internals |
