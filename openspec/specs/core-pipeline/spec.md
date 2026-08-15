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

Within a sector, stages SHALL execute in this order: conditions → mapper → pipelines/steps → exporters.

#### Scenario: Happy path with all stages

- **WHEN** a sector has a passing condition, a mapper, one step, and one exporter
- **THEN** the mapper runs only after the condition allows, the step receives the mapped notification, and the exporter receives the post-step notification

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

> Note: As of the baseline commit series, GH #7 documents a defect where an empty condition set throws `EmptyInstanceBranchException`. Main specs state the intended contract; the fix is tracked as a change.

### Requirement: Mapper is required

A sector MUST have a registered `INotificationMapper<TNotification, TEvent>`. Configuration without a mapper MUST fail at registration or startup with a clear error (not a silent no-op at runtime).

#### Scenario: Mapper missing

- **WHEN** a sector is registered without a mapper
- **THEN** the host fails registration/startup with a sector-minimal-required style error

### Requirement: Pipelines transform the notification

Steps registered on a sector (via pipelines) SHALL transform the notification after mapping. When multiple pipeline blocks are registered, the current runtime chains them: each pipeline receives the output of the previous pipeline.

#### Scenario: Two pipelines chain

- **WHEN** two `WithPipeline` blocks are registered and the first mutates the notification
- **THEN** the second pipeline observes the first pipeline's result (not a fresh copy of the mapped notification)

> Note: GH #9 tracks whether independence (fan-out + merge) should replace chaining. Until a change archives, chaining is the documented runtime behaviour.

### Requirement: Exporters receive the final notification

Every registered exporter for the sector SHALL be invoked with the final notification after conditions, mapping, and steps. Failure of one exporter MUST NOT prevent other exporters on the same sector from being attempted unless a documented decorator policy says otherwise.

#### Scenario: Two exporters

- **WHEN** two exporters are registered and the first completes successfully
- **THEN** the second is still invoked with the same final notification value

### Requirement: Sector failures are contained and observable

Exceptions during a sector's processing MUST be caught at the sector/connector boundary, logged with event and sector type context, and MUST NOT tear down the entire host process. Connector-level policy MAY rethrow after logging for a single sector failure within a parallel batch.

#### Scenario: Sector throws

- **WHEN** a mapper throws for one event
- **THEN** the failure is logged and other sectors for that event still run (or complete) according to parallel scheduling; the hosted connector continues accepting further events after the failure is handled
