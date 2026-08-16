# Core Pipeline Specification

## Purpose

Defines how a registered event type is accepted, routed to sectors, and transformed into notifications through conditions, a required mapper, optional steps, and exporters.
## Requirements
### Requirement: Event ingress via input pipe

For each registered event type `TEvent`, the host SHALL run a connector that consumes `IInputPipe<TEvent>.AcceptAsync()` as an `IAsyncEnumerable<TEvent>` until cancellation.

#### Scenario: Event arrives on the pipe

- **WHEN** a producer exports a `TEvent` into the active input pipe
- **THEN** the connector eventually observes that event and begins sector processing for it

### Requirement: One connector per event type

Registration SHALL bind a hosted connector for `TEvent` that resolves all sectors registered for that event type.

#### Scenario: Multiple sectors share one event type

- **WHEN** two sectors are registered for the same `TEvent` (different `TNotification`)
- **THEN** both sectors receive each event instance observed by the connector

### Requirement: Sectors process in parallel

The connector MUST process registered sectors for a single event concurrently (not strictly serial one-after-another), so a slow sector does not block another sector from starting.

#### Scenario: Two sectors, one slow

- **WHEN** sector A is slow and sector B is fast for the same event
- **THEN** sector B may complete while A is still running

### Requirement: Per-event DI scope per sector

Each sector invocation SHALL open its own DI scope (or equivalent) so scoped dependencies are not shared across concurrent sector runs for the same event.

#### Scenario: Scoped dependency isolation

- **WHEN** two sectors process the same event concurrently and each resolves a scoped service
- **THEN** each sector receives an instance isolated to its own scope

### Requirement: Sector processing order of stages

Within a sector, processing SHALL follow the configured graph (`When` → `Map` → `Transform`/`Branch`/`Join`/`Custom` → `Export`). The fixed linear stage order and implicit pipeline chaining of 3.1 are removed.

#### Scenario: Happy path with all stages

- **WHEN** a sector graph has a passing When node, a Map, a Transform, and an Export
- **THEN** the map runs only after the condition allows, the transform receives the mapped notification, and the exporter receives the post-transform notification

#### Scenario: Graph-defined order

- **WHEN** a sector graph maps, transforms, then fans out to two exporting branches
- **THEN** execution follows the graph edges, not a fixed stage list

### Requirement: Conditions filter events

If one or more conditions are registered, the sector MUST drop the event (no mapper/export) when any condition returns false (or the aggregate condition processor rejects).

#### Scenario: Condition rejects

- **WHEN** a condition returns false for an event
- **THEN** the mapper is not invoked and no exporter is called for that sector run

### Requirement: Empty condition set allows the event

When no conditions are registered for a sector, the sector MUST treat the event as allowed and proceed to the mapper. An empty condition set MUST NOT throw.

#### Scenario: Minimal sector with only a mapper

- **WHEN** a sector is registered with only `AddMapper` and no conditions
- **THEN** each event is mapped and may be exported without throwing due to missing conditions

#### Scenario: No EmptyInstanceBranchException for missing conditions

- **WHEN** the condition instance set is unused (`UseInstance` is false)
- **THEN** sector processing skips condition checkout and does not throw `EmptyInstanceBranchException`

### Requirement: Mapper is required

A sector MUST have a registered `INotificationMapper<TNotification, TEvent>`. Configuration without a mapper MUST fail at registration or startup with a clear error (not a silent no-op at runtime).

#### Scenario: Mapper missing

- **WHEN** a sector is registered without a mapper
- **THEN** the host fails registration/startup with a sector-minimal-required style error

### Requirement: Exporters receive the final notification

Every `Export` node reached in the graph SHALL receive the notification produced by its upstream path, and branch policy governs failure handling (see `sector-graph`).

#### Scenario: Two exporters

- **WHEN** two branches each end in an Export node
- **THEN** both exporters are invoked with their branch's notification, in parallel

### Requirement: Sector failures are contained and observable

Exceptions during a sector's processing MUST be caught at the sector/connector boundary, logged with event and sector type context, and MUST NOT tear down the entire host process. Connector-level policy MAY rethrow after logging for a single sector failure within a parallel batch.

#### Scenario: Sector throws

- **WHEN** a mapper throws for one event
- **THEN** the failure is logged and other sectors for that event still run (or complete) according to parallel scheduling; the hosted connector continues accepting further events after the failure is handled

