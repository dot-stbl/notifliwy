# Quality Engineering Specification

## Purpose

Defines expectations for unit tests, benchmarks, and package version hygiene in this repository.
## Requirements
### Requirement: Unit test project covers pipeline primitives

`test/Notifliwy.Units` SHALL contain automated tests for builders, in-memory pipes, MultiplyServiceInstance, and end-to-end sector processing sufficient to catch empty-condition and mapper-required regressions.

#### Scenario: Test run

- **WHEN** `dotnet test test/Notifliwy.Units` runs on a clean build
- **THEN** all tests pass

### Requirement: Test hosts register required DI infrastructure

Tests that activate options-bound services MUST register options (`AddOptions`) or use a host that does, so activation failures are not false negatives.

#### Scenario: In-memory pipe activation in tests

- **WHEN** a test builds a service provider for `InMemoryEventExchange` / input pipe
- **THEN** the provider resolves without missing `IOptions` / options-monitor errors

#### Scenario: Shared helper

- **WHEN** multiple unit tests need a minimal Notifliwy provider
- **THEN** they use a shared helper that includes options registration (not ad-hoc incomplete collections)

### Requirement: Benchmark project is either real or absent

`test/Notifliwy.Benchmark` MUST either contain runnable BenchmarkDotNet sources targeting a supported TFM, or be removed from the solution. An empty project that cannot run is not acceptable long-term (GH #10).

#### Scenario: Benchmark entry

- **WHEN** a contributor runs the documented benchmark command
- **THEN** either benchmarks execute or the docs/solution no longer advertise the project

### Requirement: Package version consistency

Project version properties for published packages MUST match the version intended for NuGet (or the discrepancy is documented).

#### Scenario: Core package version

- **WHEN** a reader compares `Notifliwy` / Kafka provider / OTel csproj `Version` to nuget.org
- **THEN** the versions agree or the repo documents the next publish plan

