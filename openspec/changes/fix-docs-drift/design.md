## Context

Issue #13 lists six drift points. Code wins for facts; product design issues (#9) stay deferred.

## Goals / Non-Goals

**Goals:**

- One-pass edit of listed drift items.
- BUGS.md internal consistency (detail + summary table).

**Non-Goals:**

- Full docs rewrite / OpenSpec replacing NOTIFLIWY.md.

## Decisions

1. **Core TFMs**: only net6/7/8; netstandard2.1 only for protobuf package.
2. **Bug #4**: FIXED everywhere.
3. **Connector**: describe await + `Parallel.ForEachAsync` + log/rethrow as in current code.
4. **#9**: one sentence “pipelines chain today; independence is a future extension / open design” — not “independent”.
5. **API names**: copy from source, not memory.

## Risks / Trade-offs

- Docs will again drift when #7 lands — apply docs tasks inside those changes too.
