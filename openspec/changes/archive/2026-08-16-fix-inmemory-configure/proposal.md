## Why

GitHub **#8** and **#4**: `AddInMemoryInput(configure)` does not actually configure the exchange (callback is a no-op or options never bind). Users cannot set channel capacity / full-mode; configuration for `InMemoryExchange` is missing or incomplete.

**Bug fix** (+ small enhancement to make the existing configure surface real).

## What Changes

- Wire the configure callback into real options (`IOptions` / `AddOptions`) used by `InMemoryEventExchange`.
- Expose meaningful knobs (at least capacity; full-mode if already designed).
- Document the configure API in README/docs if it becomes real.
- Tests that assert configured capacity (or options object) is applied.

## Non-goals

- New transports (Kafka already separate).
- Changing default capacity semantics beyond making them configurable.
- Fixing unrelated unit-test DI host issues except where required to test this (#11 is a separate change).

## Capabilities

### New Capabilities

_(none)_

### Modified Capabilities

- `in-memory-provider`: options integration for exchange configuration becomes normative and implemented.

## Impact

- `src/libraries/Notifliwy` in-memory pipes / DI extensions
- Unit tests under `test/Notifliwy.Units/Pipes/InMemory`
- GH #8, #4
