## Why

GitHub **#11**: unit tests build a bare `ServiceCollection` without `AddOptions`, so in-memory pipe / exchange cannot activate (`IOptions` infrastructure missing). Tests fail or cannot cover the real activation path.

**Bug fix in tests** (and optionally library self-registration if we choose to register options inside Notifliwy).

## What Changes

- Ensure test helpers / fixtures call `services.AddOptions()` (or use `Host.CreateApplicationBuilder` / equivalent).
- Optionally have Notifliwy in-memory registration call `AddOptions` so bare collections work — only if design prefers library self-sufficiency.
- Green tests for in-memory pipe activation.

## Non-goals

- Full redesign of test stack (Moq → NSubstitute, etc.).
- Implementing exchange capacity features (#8) beyond what is needed to activate.

## Capabilities

### New Capabilities

_(none)_

### Modified Capabilities

- `quality-engineering`: test hosts must register required DI infrastructure (normative).
- `in-memory-provider`: if library self-registers options, note that bare collections work.

## Impact

- `test/Notifliwy.Units/**`
- Possibly in-memory DI extension in core
- GH #11
