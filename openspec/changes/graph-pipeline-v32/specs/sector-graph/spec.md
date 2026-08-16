## Purpose

Defines the typed sector graph (G2): node vocabulary, execution semantics, branch/join behaviour, and policies for the 3.2 pipeline model.

## ADDED Requirements

### Requirement: Typed graph nodes

A sector SHALL be configured as a directed acyclic graph built from the node vocabulary `When`, `Map`, `Transform`, `Export`, `Branch`, `Join`, `Custom`. Graphs containing cycles MUST be rejected at startup with an explicit error.

#### Scenario: Cycle rejected

- **WHEN** a configured graph contains a cycle
- **THEN** host startup fails with a cycle error naming the sector

### Requirement: Map node

`Map` SHALL convert event to notification exactly once per sector run, via `INotificationMapper`, a mapping-provider adapter, or an inline lambda.

#### Scenario: Lambda map

- **WHEN** a sector uses `Map((e, ct) => new Alert(e.Id))`
- **THEN** the mapped notification flows to downstream nodes without a mapper class

### Requirement: Transform node

`Transform` SHALL map notification to notification via `INotificationTransform<TNotification>.TransformAsync` (renamed from `INotificationStep/AggregateAsync`).

#### Scenario: Chained transforms

- **WHEN** two Transform nodes run in sequence on a path
- **THEN** the second receives the first's output

### Requirement: Branch fan-out

`Branch` SHALL run its branch sub-graphs in parallel over the same input notification. `BranchPolicy.FailFast` (default) MUST rethrow the first fault; `BestEffort` MUST log per-branch failures and continue with surviving branches.

#### Scenario: BestEffort email failure

- **WHEN** a two-branch fan-out runs BestEffort and the email branch throws while slack succeeds
- **THEN** the slack branch result survives and the email error is logged

### Requirement: Join reducer

`Join` with multiple incoming branches MUST require an `INotificationJoin<TNotification>` reducer; a single-branch join is passthrough. Multi-branch join without a reducer MUST fail at registration.

#### Scenario: Two branches reduced

- **WHEN** two branches join with a reducer
- **THEN** the reducer receives both outputs and produces the notification passed downstream

### Requirement: Custom escape node

`Custom` SHALL accept an inline `Func<TNotification, CancellationToken, ValueTask<TNotification>>` or an `INotificationCustom<TNotification>` DI class.

#### Scenario: Reusable custom class

- **WHEN** `Custom<RateLimitGate>()` is used in two sectors
- **THEN** the DI-registered class is shared per its lifetime
