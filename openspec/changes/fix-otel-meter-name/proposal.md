## Why

GitHub **#6**: `AddNotifliwyServerInstrumentation()` subscribes by an instrument/meter name that does not match what the core library creates, so **no metrics are collected**.

**Bug fix** — wire names must match.

## What Changes

- Align OTel instrumentation registration with `DiagnosticMeter` / `ActivitySource` names in core.
- Add a smoke test or doc note proving names match (prefer a unit/integration assertion if cheap).
- No intentional public API rename unless the published name was never observed (document if **BREAKING** for anyone who subscribed manually to the wrong name).

## Non-goals

- Full metrics dashboard / custom views.
- Changing which counters exist (only subscription correctness).

## Capabilities

### New Capabilities

_(none)_

### Modified Capabilities

- `opentelemetry`: instrumentation package registers by real instrument names (enforce after fix).

## Impact

- `src/diagnostic/Notifliwy.OpenTelemetry.Instrumentation`
- `src/libraries/Notifliwy/Diagnostic/*`
- GH #6
