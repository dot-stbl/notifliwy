## Purpose

Defines how a registered event type is accepted, routed to sectors, and transformed into notifications through the 3.2 graph model.

## MODIFIED Requirements

### Requirement: Sector processing order of stages

Within a sector, processing SHALL follow the configured graph (`When` → `Map` → `Transform`/`Branch`/`Join`/`Custom` → `Export`). The fixed linear stage order and implicit pipeline chaining of 3.1 are removed.

#### Scenario: Happy path with all stages

- **WHEN** a sector graph has a passing When node, a Map, a Transform, and an Export
- **THEN** the map runs only after the condition allows, the transform receives the mapped notification, and the exporter receives the post-transform notification

#### Scenario: Graph-defined order

- **WHEN** a sector graph maps, transforms, then fans out to two exporting branches
- **THEN** execution follows the graph edges, not a fixed stage list

### Requirement: Exporters receive the final notification

Every `Export` node reached in the graph SHALL receive the notification produced by its upstream path, and branch policy governs failure handling (see `sector-graph`).

#### Scenario: Two exporters

- **WHEN** two branches each end in an Export node
- **THEN** both exporters are invoked with their branch's notification, in parallel

## REMOVED Requirements

### Requirement: Pipelines transform the notification

Superseded by `sector-graph` Transform/Branch nodes. The 3.1 `INotificationPipeline` chain and `WithPipeline` API are deleted; migration guidance lives in `docs/MIGRATION-3.2.md`.
