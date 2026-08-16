## Context

Core defines `DiagnosticMeter` / `DiagnosticActivity` with specific names. Instrumentation package adds meters/sources by a different string → empty export.

## Goals / Non-Goals

**Goals:**

- Single source of truth for instrument names (constants shared or duplicated with a test that compares).
- Metrics flow when the sample/server enables OTel + instrumentation.

**Non-Goals:**

- New metric dimensions / high-cardinality tags.

## Decisions

1. Prefer **shared public constants** from core (or `InternalsVisibleTo` + test) so names cannot drift.
2. Fix subscription strings first; extract constants if easy without package cycle issues.
3. If external users already subscribed to the *wrong* name, document the fix as correcting dead config (not a real breaking change).

## Risks / Trade-offs

- Package reference direction: instrumentation → core already; constants live in core.
