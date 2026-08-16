## Purpose

Defines config-class sector registration, inline registration, and assembly discovery for 3.2.

## ADDED Requirements

### Requirement: Config-class sector

A sector SHALL be definable as an `INotificationSectorConfig<TNotification, TEvent>` class exposing execution options (e.g. `SectorExecution`) and a `Configure(ISectorGraphBuilder<…>)` method, registered via `AddSector<TConfig>()`.

#### Scenario: One-line DI

- **WHEN** a host registers `server.AddSector<PaymentSector>()`
- **THEN** the sector graph from the config class is active with no other lambda block

### Requirement: Inline sector registration

For one-off sectors a host SHALL be able to register `AddSector<TNotification, TEvent>(g => …)` with a graph lambda instead of a class.

#### Scenario: Sample sector inline

- **WHEN** a sample registers an inline 3-node graph
- **THEN** behaviour matches the equivalent config class

### Requirement: Discovery

`AddSectorsFromAssembly` (reflection fallback) SHALL be opt-in and emit a startup warning; the source-generated `[NotifliwySectors]` registration is the primary path with no runtime reflection.

#### Scenario: Generated registration

- **WHEN** an assembly has `[NotifliwySectors]` and the host calls the generated extension
- **THEN** all config classes are registered without reflection scanning

#### Scenario: Fallback warning

- **WHEN** a host opts into `AddSectorsFromAssembly`
- **THEN** a warning is logged and sectors are still discovered
