## Why

GitHub **#13**: documentation has drifted from the code in six places (TFMs, Bug #4 status, fire-and-forget connector prose, API typos, wrong builder in examples, pipeline independence wording).

**Docs-only fix** — code is treated as source of truth except where a separate code issue exists (#9).

## What Changes

- Align `CLAUDE.md`, `docs/BUGS.md`, `docs/NOTIFLIWY.md` (and README if needed) with current code.
- Fix netstandard2.1 claim for core; Bug #4 FIXED consistently; connector concurrency prose; `ConfigureNotifliwyPipe` spelling; `AddExporter` on sector builder; point pipeline independence to #9 / OpenSpec deferral.

## Non-goals

- Implementing #7/#8/#9 behaviour changes (separate changes).
- Rewriting the marketing README tone.

## Capabilities

### New Capabilities

_(none)_

### Modified Capabilities

- `documentation`: requirements already describe correct doc hygiene; this change applies them (delta only if we need to mark specific files).

## Impact

- `CLAUDE.md`, `docs/*`, possibly `README.md`
- GH #13

`skip_specs` is **not** set: documentation capability is modified only if we strengthen a requirement; pure editorial may use skip — we include a thin delta confirming TFM/API accuracy.
