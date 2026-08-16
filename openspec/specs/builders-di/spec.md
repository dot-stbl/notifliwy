# Builders and DI Specification

## Purpose

Defines the fluent registration surface (`AddNotifliwyServer`, sector builder methods) and how pipeline components are registered in the Microsoft.Extensions.DependencyInjection container.
## Requirements
### Requirement: Server registration entry point

Consumers MUST be able to register Notifliwy via an extension on `IServiceCollection` named `AddNotifliwyServer` that accepts a configuration callback over a server builder.

#### Scenario: Minimal host wiring

- **WHEN** a host calls `services.AddNotifliwyServer(server => { … })`
- **THEN** connectors and sectors configured in the callback are available to the generic host as hosted services / resolvable types

### Requirement: Sector registration pairs notification and event

The server builder SHALL provide `AddSector<TConfig>()` for `INotificationSectorConfig<TNotification, TEvent>` classes and `AddSector<TNotification, TEvent>(g => …)` for inline graph lambdas. The 3.1 `AddNotification` fluent block is removed.

#### Scenario: Register one sector

- **WHEN** `AddSector<PaymentSector>()` is called
- **THEN** a sector mapping its event to its notification is registered for that connector

### Requirement: Pipeline components are DI services

Conditions, mappers, steps, and exporters registered through the builder MUST be resolvable from the DI container (typically as transient) so they can take constructor dependencies.

#### Scenario: Mapper with a dependency

- **WHEN** a mapper type requires `ILogger<T>` in its constructor and is registered via `AddMapper<TMapper>`
- **THEN** the sector can resolve the mapper from a scope without manual construction

### Requirement: Multiple sectors for one event type

The server builder MUST allow multiple `AddNotification` calls that share the same `TEvent` and different `TNotification` types.

#### Scenario: Fan-out by sector

- **WHEN** two sectors are registered for `OrderPlaced` producing `EmailNotice` and `SmsNotice`
- **THEN** both sectors are attached to the `OrderPlaced` connector

### Requirement: Input provider registration is separate from sectors

Input pipes (in-memory or external providers) SHALL be registered via dedicated server-builder methods (for example `AddInMemoryInput`) rather than implied by sector registration alone.

#### Scenario: Sectors without input

- **WHEN** sectors are registered but no input pipe is registered for `TEvent`
- **THEN** the connector for `TEvent` cannot usefully accept events until an input is registered (startup or runtime error is acceptable; silent hang is not preferred)

