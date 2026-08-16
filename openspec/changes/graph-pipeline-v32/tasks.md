## 1. Core graph model

- [ ] 1.1 `ISectorGraphBuilder<TNotification, TEvent>` with When/Map/Transform/Export/Branch/Join/Custom
- [ ] 1.2 Graph plan model + cycle validation (startup error)
- [ ] 1.3 `INotificationTransform<TNotification>` (rename from Step, `TransformAsync`)
- [ ] 1.4 `INotificationJoin<TNotification>` reducer + single-branch passthrough
- [ ] 1.5 `INotificationCustom<TNotification>` + inline lambda Custom node
- [ ] 1.6 Branch parallel execution with `BranchPolicy.FailFast|BestEffort`

## 2. Registration surface

- [ ] 2.1 `INotificationSectorConfig<TNotification, TEvent>` + `AddSector<TConfig>()`
- [ ] 2.2 Inline `AddSector<TNotification, TEvent>(g => …)`
- [ ] 2.3 Remove 3.1 fluent sector API + multi-WithPipeline (V3)
- [ ] 2.4 `docs/MIGRATION-3.2.md` mapping every removed call to graph equivalent

## 3. Source generators

- [ ] 3.1 Generator B: `[NotifliwySectors]` → generated `AddNotifliwySectors…()` registration
- [ ] 3.2 Runtime `AddSectorsFromAssembly()` opt-in fallback with warning
- [ ] 3.3 Generator C: compiled sector invoke for singleton/stateless graphs
- [ ] 3.4 `SectorExecution.Auto|Compiled|Scoped` + fail-fast captive guard

## 4. Mapping providers

- [ ] 4.1 `Notifliwy.Mapping.Mapperly` adapter package (blessed)
- [ ] 4.2 `Notifliwy.Mapping.Mapster` adapter package
- [ ] 4.3 Docs: when to use provider vs lambda `Map`

## 5. Tests (M3 coverage requirement)

- [ ] 5.1 Unit: each node type behaviour
- [ ] 5.2 Unit: graph compose — order, branch fan-out, join reduce, custom
- [ ] 5.3 Unit: cycle rejection, multi-branch join without reducer error, Compiled+scoped fail-fast
- [ ] 5.4 Dual-path: Compiled vs Scoped observable equality
- [ ] 5.5 BranchPolicy: FailFast rethrow vs BestEffort continue
- [ ] 5.6 End-to-end: sector config class → events → exporters

## 6. Docs/samples/teardown

- [ ] 6.1 Update in-memory + kafka samples to config classes + graph
- [ ] 6.2 README graph section; close GH #9 and #2 with design rationale
- [ ] 6.3 Benchmarks: graph path vs 3.1-style baseline; Branch parallel scaling
- [ ] 6.4 Archive this change; update main specs
