# Kafka MassTransit Provider Specification

## Purpose

Defines the MassTransit Kafka provider package that adapts Kafka consumption into Notifliwy input pipes and documents the registration helpers.

## Requirements

### Requirement: Separate provider package

Kafka integration MUST live in `Notifliwy.Provider.MassTransit.Kafka` so the core package does not take MassTransit/Kafka dependencies.

#### Scenario: Core without Kafka

- **WHEN** a consumer references only `Notifliwy`
- **THEN** MassTransit and Confluent Kafka packages are not required to compile

### Requirement: Consumer pipe bridges MassTransit to Notifliwy

The provider SHALL expose a consumer/pipe that receives Kafka messages via MassTransit and feeds them into Notifliwy's input path (directly or via an internal buffer compatible with connectors).

#### Scenario: Message on topic

- **WHEN** a message of type `TEvent` is consumed from the configured Kafka topic
- **THEN** registered Notifliwy sectors for `TEvent` process that event

### Requirement: Registration helpers have correct public names

Public extension methods for registration and endpoint configuration MUST use the documented PascalCase names (for example `ConfigureNotifliwyPipe`), without typos that diverge from the implementation.

#### Scenario: Sample server wires Kafka

- **WHEN** the Kafka sample server configures the rider/endpoint using the provider extensions
- **THEN** the code compiles against the public API names in the package

### Requirement: Sample demonstrates end-to-end path

The repository SHOULD ship a Kafka sample (server + sender) that can run against a local compose stack and demonstrate at least one sector processing a published event.

#### Scenario: Compose + server + sender

- **WHEN** Kafka is available via the sample compose file and the server and sender projects run
- **THEN** a published sample event results in observable sector/exporter activity on the server
