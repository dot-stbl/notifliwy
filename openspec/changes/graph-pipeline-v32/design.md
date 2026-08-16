## Context

Grill session 2026-08-16 (owner decisions in parentheses). 3.1 semantics: sectors parallel, pipelines chain, exporters serial; per-event DI scope; in-memory channel default 1M capacity.

## Goals / Non-Goals

**Goals:** ship G2 graph + config classes + provider mappers + compiled path in one major (3.2), close #9/#2 by design, beat MediatR ergonomics in the notification-projection niche.

**Non-Goals:** cycles, CEP windows, new transports, in-house mapper generator.

## Decisions

1. **V3 hard break.** Old fluent API removed in 3.2; `docs/MIGRATION-3.2.md` in the same release. Rationale: two parallel APIs for one concept is the MediatR mess we are escaping.
2. **R2 registration.** `INotificationSectorConfig<TNotification, TEvent>` with `SectorExecution` property and `Configure(ISectorGraphBuilder<…>)`; plus inline `AddSector<TNotification, TEvent>(Action<ISectorGraphBuilder<…>>)` for samples.
3. **M3 node set.** When / Map / Transform / Export / Branch / Join / Custom. Graph validator rejects cycles at startup. Every node type gets unit tests; graph composition tests cover ordering, branch, join, custom resolution.
4. **N2 rename.** `INotificationTransform<TNotification>` with `TransformAsync`; node `Transform<T>()`. `AggregateAsync` was a misnomer.
5. **J1 join.** `INotificationJoin<TNotification>` reducer with `Join(IReadOnlyList<TNotification>, CancellationToken) → ValueTask<TNotification>`; single-branch join = passthrough; multi-branch without reducer = registration error.
6. **P mapping providers.** `Notifliwy.Mapping.Mapperly` and `Notifliwy.Mapping.Mapster` adapter packages implement mapping from provider output to `INotificationMapper`. Core takes no dependency on either.
7. **H1b execution.** `SectorExecution.Auto` picks compiled when all stages resolve singleton/stateless; `Compiled` forces it and fails fast on scoped deps (captive-dependency guard); `Scoped` forces per-event scope. Startup log states chosen path per sector.
8. **B3 discovery.** Source generator emits registration extension from `[NotifliwySectors]`; `AddSectorsFromAssembly()` reflection fallback is opt-in and logs a warning.
9. **F2 branch policy.** Branches run in parallel (`Task.WhenAll`); `BranchPolicy.FailFast` (default) rethrows first fault; `BestEffort` logs per-branch errors and joins survivors. Policy settable per Branch and per sector config.

## Risks / Trade-offs

- Hard break churn for existing consumers → mitigated by migration guide + samples updated in-release.
- Two new packages to maintain → adapters are thin; provider majors are the exposure.
- Compiled path divergence risk → same observable behaviour asserted by dual-path tests (Compiled vs Scoped).
