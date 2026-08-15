# Documentation Specification

## Purpose

Defines what must stay true about project documentation relative to the code and NuGet packages.

## Requirements

### Requirement: Target frameworks match the csproj

Any doc that lists TFMs for the core library MUST match `Notifliwy.csproj` (`net6.0;net7.0;net8.0` unless the project changes). netstandard2.1 MUST NOT be claimed for core if only the protobuf extension targets it.

#### Scenario: CLAUDE/README TFM list

- **WHEN** a reader compares CLAUDE.md / README framework claims to `Notifliwy.csproj`
- **THEN** the sets are identical

### Requirement: API names in docs match public APIs

Documented method names (for example `ConfigureNotifliwyPipe`, `AddExporter` on the sector builder) MUST match the public surface. Typos and wrong-builder examples are defects.

#### Scenario: Copy-paste from docs compiles

- **WHEN** a sample snippet from docs/NOTIFLIWY.md or CLAUDE.md is pasted into a host project with the right packages
- **THEN** referenced APIs resolve (modulo types the reader must implement)

### Requirement: Minimal examples match runtime behaviour

The documented minimal setup (mapper-only sector, optional condition) MUST work as described. If the runtime differs, either the code or the docs MUST be fixed in the same change cycle.

#### Scenario: Mapper-only sector

- **WHEN** a user follows the minimal setup without a condition
- **THEN** events are processed without silent empty-condition failures

### Requirement: Bug tracker status is consistent

`docs/BUGS.md` MUST not claim both FIXED and OPEN for the same item. Summary tables MUST match per-bug status lines.

#### Scenario: Bug #4 status

- **WHEN** AggregateAsync already uses ValueTask in code
- **THEN** BUGS.md marks that item fixed in both the detail section and the summary table

### Requirement: Connector concurrency description matches code

Docs that describe fire-and-forget `Task.Run` per sector MUST be updated if the connector awaits `Parallel.ForEachAsync` (or vice versa).

#### Scenario: Connector section vs NotificationConnector.cs

- **WHEN** a reader compares the connector prose to the implementation
- **THEN** the concurrency and error-handling story matches the code
