## Why

Notifliwy 3.1 has a fixed linear pipeline (conditions → mapper → chained pipelines → serial exporters). GitHub #9 exposed that multi-`WithPipeline` semantics are ambiguous, and the current surface forces long lambda blocks in DI registration. To compete with MediatR-class ergonomics we need: an arbitrary typed graph, config-class registration, compile-time mapping via provider packages, and a compiled hot path.

## What Changes

- **BREAKING (V3):** remove the 3.1 fluent sector API (`AddCondition/AddMapper/WithPipeline/AddExporter` chains and multi-`WithPipeline`). A migration guide ships in the same release.
- New **G2 graph API** on a sector: `When<T>()`, `Map<T>()` / `Map((e,ct)=>…)`, `Transform<T>()` (replaces `INotificationStep`), `Export<T>()`, `Branch(...)`, `Join<T>()`, `Custom(...)` (M3 full set; cycles forbidden).
- **R2 registration:** `INotificationSectorConfig<TNotification, TEvent>` classes + short inline `AddSector<TNotification, TEvent>(g => …)` for one-offs.
- **Mapping providers (P):** two adapter packages — `Notifliwy.Mapping.Mapperly` (source-gen, blessed) and `Notifliwy.Mapping.Mapster` (runtime). Core stays mapper-agnostic; no in-house mapper generator.
- **Rename (N2):** `INotificationStep` → `INotificationTransform`, `AggregateAsync` → `TransformAsync`, graph node `Transform<T>()`.
- **Join (J1):** `INotificationJoin<T>` reducer required for multi-branch joins; single-branch join is passthrough.
- **Custom (C3):** inline lambda `Func<T, CancellationToken, ValueTask<T>>` + `INotificationCustom<T>` DI class.
- **Branch execution (F2):** parallel branches with `BranchPolicy = FailFast | BestEffort` (default FailFast).
- **Execution mode (H1b):** `SectorExecution = Auto | Compiled | Scoped`; `Compiled` with scoped dependencies fails fast at registration.
- **Discovery (B3):** source-gen `[NotifliwySectors]` registration primary; opt-in runtime assembly scan fallback emits a warning.

## Non-goals

- Cycles / loops in the graph (rejected by validation).
- Windows, sessions, multi-event joins (CEP territory).
- Rewriting transports; Kafka/in-memory pipes unchanged.
- In-house mapping source-generator.

## Capabilities

### New Capabilities

- `sector-graph`: G2 typed graph nodes, branch/join execution semantics, policies.
- `sector-config`: config-class and inline sector registration, discovery (source-gen + fallback).
- `compiled-execution`: H1b execution modes and compiled hot path.
- `mapping-providers`: Mapperly/Mapster adapter packages contract.

### Modified Capabilities

- `core-pipeline`: stage vocabulary rename (Transform), pipeline-chain requirement replaced by explicit Branch/Join; exporter serialization policy via BranchPolicy.
- `builders-di`: `AddNotification` fluent block replaced by `AddSector` + graph; options registration unchanged.

## Impact

- Core library public surface (breaking), new packages ×2, samples, README/docs/CLAUDE migration notes, benchmarks (add graph vs 3.1 path), GH #9 and #2 closed by design.
