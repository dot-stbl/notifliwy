## MODIFIED Requirements

### Requirement: Test hosts register required DI infrastructure

Tests that activate options-bound services MUST register options (`AddOptions`) or use a host that does, so activation failures are not false negatives.

#### Scenario: In-memory pipe activation in tests

- **WHEN** a test builds a service provider for `InMemoryEventExchange` / input pipe
- **THEN** the provider resolves without missing `IOptions` / options-monitor errors

#### Scenario: Shared helper

- **WHEN** multiple unit tests need a minimal Notifliwy provider
- **THEN** they use a shared helper that includes options registration (not ad-hoc incomplete collections)
