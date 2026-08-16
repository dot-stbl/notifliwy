# In-Memory Provider Specification

## Purpose

Defines the built-in in-process transport: channel-backed exchange, input pipe, export pipe, and configuration surface for capacity/options.
## Requirements
### Requirement: Built-in registration

The core library SHALL provide `AddInMemoryInput` (or equivalent) on the server builder that registers an in-memory input pipe and exchange for the event types used by registered sectors.

#### Scenario: Sample host

- **WHEN** a host calls `AddNotifliwyServer` with `AddInMemoryInput` and one sector
- **THEN** injecting `IExportPipe<TEvent>` and calling export delivers events to the sector without an external broker

### Requirement: Channel-backed exchange

In-memory transport SHALL use a channel (or equivalent async queue) so producers and the connector consumer are decoupled.

#### Scenario: Producer before connector ready

- **WHEN** a producer exports events before or while the connector is running
- **THEN** events are buffered according to channel capacity policy and later accepted by the connector

### Requirement: Export pipe is the producer API

Producers MUST publish events through `IExportPipe<TEvent>.ExportAsync` (or the documented export API), not by writing to the channel directly from application code.

#### Scenario: Unit test producer

- **WHEN** a test resolves `IExportPipe<TEvent>` and exports one event
- **THEN** the corresponding input pipe eventually yields that event

### Requirement: Options integration for exchange configuration

Registration that accepts a configure callback MUST bind options (capacity, full-mode, etc.) so the callback is not a no-op. Options registration MUST include `IOptions` / `AddOptions` plumbing required for activation.

#### Scenario: Configure capacity

- **WHEN** the consumer passes a configure action that sets channel capacity
- **THEN** the running exchange uses that capacity

#### Scenario: Configure is not a no-op

- **WHEN** two hosts register with different capacity values
- **THEN** their exchanges do not silently share a single hard-coded capacity ignoring both callbacks

### Requirement: Test hosts must register options services

Any host or test that builds a `ServiceCollection` for in-memory pipes MUST include options services (`AddOptions` / default host plumbing) required by the exchange so activation does not fail with missing `IOptions` infrastructure.

#### Scenario: Bare ServiceCollection

- **WHEN** unit tests register Notifliwy in-memory pieces on a bare `ServiceCollection` without options
- **THEN** either the library registers options for them, or tests document the required `AddOptions` call (GH #11)

