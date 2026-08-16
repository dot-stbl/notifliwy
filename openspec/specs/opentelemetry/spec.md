# OpenTelemetry Instrumentation Specification

## Purpose

Defines how Notifliwy exposes ActivitySource/Meter signals and how the instrumentation package hooks them into an OpenTelemetry pipeline.
## Requirements
### Requirement: Activity source for connector and sector work

The core library SHALL open activities for connector-level event handling and sector processing, tagged with event (and where applicable notification) type information.

#### Scenario: Trace a single event

- **WHEN** an event is processed with an active ActivityListener/OTel exporter
- **THEN** activities for the connector path appear with event-type tags

### Requirement: Meter for input volume

The core library SHALL expose a meter/counter for accepted input events so hosts can scrape or export throughput.

#### Scenario: Counter increments

- **WHEN** the connector successfully schedules processing for an event
- **THEN** the input counter increments by one with event-type tags

### Requirement: Instrumentation package registers by real instrument names

`AddNotifliwyServerInstrumentation` (or equivalent) MUST subscribe using the same instrument/source names that the core library creates. Subscribing by a mismatched name MUST NOT be considered successful configuration.

#### Scenario: Metrics actually flow

- **WHEN** a host calls the instrumentation extension and processes events
- **THEN** OTel metrics export includes Notifliwy input counters (not an empty subscription)

#### Scenario: Name parity

- **WHEN** core meter name and instrumentation subscription name are compared
- **THEN** they are identical strings (shared constant or tested equality)

### Requirement: Separate instrumentation package

OpenTelemetry wiring helpers MUST live in `Notifliwy.OpenTelemetry.Instrumentation` so core does not require OTel SDK packages for basic use.

#### Scenario: Core without OTel package

- **WHEN** a consumer uses only `Notifliwy` without the instrumentation package
- **THEN** the library still processes events; activities may simply go unobserved

