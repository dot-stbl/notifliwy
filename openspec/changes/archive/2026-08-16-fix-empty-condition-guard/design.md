## Context

`SectorBlock.ProcessingAsync` always calls `ConditionInstances.CheckoutInstanceAsync`. `MultiplyServiceInstance` throws `EmptyInstanceBranchException` when `UseInstance` is false. Pipelines and exporters already skip when unused; conditions do not.

## Goals / Non-Goals

**Goals:**

- No conditions ⇒ allow event and continue to mapper.
- Keep throw when code *expects* instances but the set is empty in other call paths (tests that assert throw on empty multiply set stay valid).

**Non-Goals:**

- Default “always true” condition type registered automatically (prefer guard, not fake registration).
- Changing condition processor aggregation rules.

## Decisions

1. **Guard in SectorBlock** before `CheckoutInstanceAsync` for conditions — mirror pipeline/exporter `if (…UseInstance)` pattern. Smallest fix, no API change.
2. **Do not** remove the throw from `MultiplyServiceInstance` for empty sets in general — other code may rely on fail-fast when instances were expected.

## Risks / Trade-offs

- Any caller that *wanted* empty conditions to fail will now process events — that matches docs; treat as intentional behaviour change for misconfigured-but-documented setups.
