## Context

`AddInMemoryInput` accepts (or should accept) a configure action; today it cannot change exchange behaviour. Exchange likely uses a large fixed channel capacity.

## Goals / Non-Goals

**Goals:**

- Configure callback mutates options that the exchange reads at construction.
- `AddOptions` + bind so activation works in host and tests.
- At least one documented option (e.g. capacity) with a test.

**Non-Goals:**

- Dynamic reconfiguration at runtime after the connector started.
- Full backpressure product story.

## Decisions

1. Prefer **`IOptions<T>` / `IOptionsMonitor`** pattern already common in .NET hosts.
2. Keep defaults compatible with current reliability-oriented large capacity unless a clear reason to shrink.
3. If the public signature already has `configure` but ignores it, fix in place (**not BREAKING**). If no overload exists, add optional configure overload (**not BREAKING**).

## Risks / Trade-offs

- Tests must register options (#11) — coordinate with `fix-test-options-host` or include minimal `AddOptions` in the same PR if blocked.
