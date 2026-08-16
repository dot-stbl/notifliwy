## 1. Diff names

- [x] 1.1 Record meter/activity names in core `DiagnosticMeter` / `DiagnosticActivity`
- [x] 1.2 Record names used in `AddNotifliwyServerInstrumentation`
- [x] 1.3 List mismatches

## 2. Fix

- [x] 2.1 Point instrumentation at real names (prefer constants from core)
- [x] 2.2 Optional: small test asserting string equality / meter creation

## 3. Verify

- [x] 3.1 Build diagnostic + core projects
- [ ] 3.2 Close GH #6

### Findings (1.1–1.3)

| Source | Name |
|---|---|
| `DiagnosticMeter.NotifliwyServerMeter` (meter name) | `Notifliwy.Server` (`DiagnosticMeter.cs`, `$"{nameof(Notifliwy)}.Server"`) |
| `DiagnosticMeter.InputCounter` (instrument name) | `notifliwy.server.event.count` |
| `DiagnosticActivity.NotifliwySource` (activity source name) | core assembly full name (`DiagnosticActivity.InstrumentName`) |

Mismatch: `MetricBuilderExtensions.AddNotifliwyServerInstrumentation` passed
`DiagnosticMeter.InputCounter.Name` (instrument name) to `AddMeter`, which matches
on the **meter** name — no metrics were exported. `TraceBuilderExtensions` was
correct (subscribes via `NotifliwySource.Name`).

### Fix (2.1–2.2)

- Extracted `DiagnosticMeter.MeterName` const in core; `CreateMeter` uses it.
- `MetricBuilderExtensions` now calls `AddMeter(DiagnosticMeter.MeterName)`.
- Tracing side untouched (already reads the live source name).
- Added `DiagnosticMeterTests` pinning the wire name + constant↔meter sync.
