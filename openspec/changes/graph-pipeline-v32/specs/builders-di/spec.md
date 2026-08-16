## Purpose

Defines the registration surface (`AddNotifliwyServer`, sector registration) and DI container integration for 3.2.

## MODIFIED Requirements

### Requirement: Sector registration pairs notification and event

The server builder SHALL provide `AddSector<TConfig>()` for `INotificationSectorConfig<TNotification, TEvent>` classes and `AddSector<TNotification, TEvent>(g => …)` for inline graph lambdas. The 3.1 `AddNotification` fluent block is removed.

#### Scenario: Register one sector

- **WHEN** `AddSector<PaymentSector>()` is called
- **THEN** a sector mapping its event to its notification is registered for that connector

## REMOVED Requirements

### Requirement: Fluent stage methods on the sector builder

Stage wiring now happens in the graph builder (`ISectorGraphBuilder`); there is no linear sector builder with `AddCondition/AddMapper/WithPipeline/AddExporter` in 3.2. Migration guidance lives in `docs/MIGRATION-3.2.md`.
