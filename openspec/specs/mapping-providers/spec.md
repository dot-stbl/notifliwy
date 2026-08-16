# mapping-providers Specification

## Purpose
Defines the mapping-provider adapter packages contract: Mapperly (source-gen, blessed) and Mapster (runtime).
## Requirements
### Requirement: Provider adapter packages

Mapping SHALL be pluggable via `Notifliwy.Mapping.Mapperly` and `Notifliwy.Mapping.Mapster` adapter packages that produce `INotificationMapper` implementations. The core package MUST NOT reference either provider.

#### Scenario: Core without providers

- **WHEN** a consumer references only core
- **THEN** no Mapperly/Mapster dependency is required

### Requirement: Mapperly blessed default

Documentation SHALL recommend Mapperly as the default provider for compile-time mapping; Mapster is supported for existing Mapster users.

#### Scenario: Provider swap

- **WHEN** a sector switches `Map<TMapper>()` from a Mapperly-generated mapper to a Mapster-backed one
- **THEN** the graph runs unchanged

