# compiled-execution Specification

## Purpose
Defines execution modes for sector runs: compiled hot path vs per-event DI scope, and the captive-dependency guard.
## Requirements
### Requirement: Execution modes

Each sector SHALL have `SectorExecution = Auto | Compiled | Scoped`. `Auto` selects the compiled path when every stage resolves singleton/stateless, otherwise the scoped path. The chosen path MUST be logged at startup per sector.

#### Scenario: Auto picks compiled

- **WHEN** a sector's stages are all singleton/stateless and mode is Auto
- **THEN** the compiled invoke path is used and logged

### Requirement: Compiled captive guard

`SectorExecution.Compiled` on a graph with scoped dependencies MUST fail fast at registration with an explicit captive-dependency error.

#### Scenario: Compiled with scoped dep

- **WHEN** a transform requires a scoped service and mode is Compiled
- **THEN** startup fails naming the scoped dependency

### Requirement: Behavioural parity

Compiled and scoped paths MUST be observably identical for the same graph and inputs.

#### Scenario: Dual-path test

- **WHEN** the same sector graph runs under both modes
- **THEN** exporter outputs and error behaviour match

