## MODIFIED Requirements

### Requirement: Instrumentation package registers by real instrument names

`AddNotifliwyServerInstrumentation` (or equivalent) MUST subscribe using the same instrument/source names that the core library creates. Subscribing by a mismatched name MUST NOT be considered successful configuration.

#### Scenario: Metrics actually flow

- **WHEN** a host calls the instrumentation extension and processes events
- **THEN** OTel metrics export includes Notifliwy input counters (not an empty subscription)

#### Scenario: Name parity

- **WHEN** core meter name and instrumentation subscription name are compared
- **THEN** they are identical strings (shared constant or tested equality)
