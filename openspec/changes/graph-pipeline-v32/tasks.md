## 1. Core graph model

- [x] 1.1 `ISectorGraphBuilder<TNotification, TEvent>` with When/Map/Transform/Export/Branch/Join/Custom
- [x] 1.2 Graph plan model + cycle validation (startup error)
- [x] 1.3 `INotificationTransform<TNotification>` (rename from Step, `TransformAsync`)
- [x] 1.4 `INotificationJoin<TNotification>` reducer + single-branch passthrough
- [x] 1.5 `INotificationCustom<TNotification>` + inline lambda Custom node
- [x] 1.6 Branch parallel execution with `BranchPolicy.FailFast|BestEffort`

## 2. Registration surface

- [x] 2.1 `INotificationSectorConfig<TNotification, TEvent>` + `AddSector<TConfig>()`
- [x] 2.2 Inline `AddSector<TNotification, TEvent>(g => …)`
- [x] 2.3 Remove 3.1 fluent sector API + multi-WithPipeline (V3)
- [x] 2.4 `docs/MIGRATION-3.2.md` mapping every removed call to graph equivalent

## 3. Source generators

- [x] 3.1 Generator B: `[NotifliwySectors]` → generated `AddNotifliwySectors…()` registration
- [x] 3.2 Runtime `AddSectorsFromAssembly()` opt-in fallback with warning
- [x] 3.3 Generator C: compiled sector invoke for singleton/stateless graphs
- [x] 3.4 `SectorExecution.Auto|Compiled|Scoped` + fail-fast captive guard

## 4. Mapping providers

- [x] 4.1 `Notifliwy.Mapping.Mapperly` adapter package (blessed)
- [x] 4.2 `Notifliwy.Mapping.Mapster` adapter package
- [x] 4.3 Docs: when to use provider vs lambda `Map`

## 5. Tests (M3 coverage requirement)

- [x] 5.1 Unit: each node type behaviour
- [x] 5.2 Unit: graph compose — order, branch fan-out, join reduce, custom
- [x] 5.3 Unit: cycle rejection, multi-branch join without reducer error, Compiled+scoped fail-fast (graph structure validation done in Wave 1: Map missing/duplicate/order, dead-end branch, Join without Branch; remaining parts land with registration surface + Compiled mode)
- [x] 5.4 Dual-path: Compiled vs Scoped observable equality
- [x] 5.5 BranchPolicy: FailFast rethrow vs BestEffort continue
- [ ] 5.6 End-to-end: sector config class → events → exporters

## 6. Docs/samples/teardown

- [x] 6.1 Update in-memory + kafka samples to config classes + graph
- [x] 6.2 README graph section; close GH #9 and #2 with design rationale (GH issues themselves are closed by the orchestrator after merge — README/MIGRATION-3.2 now document the closing rationale)
- [x] 6.3 Benchmarks: graph path vs 3.1-style baseline; Branch parallel scaling (CompiledVsScopedBenchmarks added alongside the existing sector benchmark; dry run verified)
- [ ] 6.4 Archive this change; update main specs
