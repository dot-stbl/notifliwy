## Why

GitHub **#7**: a sector registered with only a mapper (no conditions) throws `EmptyInstanceBranchException` on every event; the exception is swallowed, so nothing is exported. Docs and the minimal README example claim conditions are optional.

This is a **bug fix** — intended behaviour is already stated in `openspec/specs/core-pipeline` (“empty condition set allows”).

## What Changes

- Guard condition checkout in `SectorBlock` the same way as pipelines/exporters: if no condition instances, treat as allow.
- Add a unit/integration test for mapper-only sector that receives an event and reaches the exporter.
- No public API change.

## Non-goals

- Changing multi-condition AND/OR semantics when conditions *are* registered.
- Pipeline independence (#9) or other sector features.

## Capabilities

### New Capabilities

_(none)_

### Modified Capabilities

- `core-pipeline`: confirm/enforce “empty condition set allows” as runtime behaviour (delta if any wording needs strengthening after the fix).

## Impact

- `src/libraries/Notifliwy/Contexts/SectorBlock.cs`
- Possibly `MultiplyServiceInstance` call sites only (prefer guard in SectorBlock, not weakening empty-set throw elsewhere)
- `test/Notifliwy.Units` — new or extended e2e/sector test
- Closes #7 when applied
