## Context

In-memory exchange depends on options. Tests construct `ServiceCollection` manually and omit `AddOptions`.

## Goals / Non-Goals

**Goals:**

- In-memory activation succeeds in unit tests.
- One shared test helper for “minimal Notifliwy service provider” to avoid copy-paste.

**Non-Goals:**

- Integration Testcontainers hosts.
- Changing production defaults.

## Decisions

1. **Primary fix in tests**: always `AddOptions()` in the shared fixture/helper.
2. **Secondary (optional)**: library registration also calls `services.AddOptions()` when registering in-memory — defensive for bare collections; low cost.
3. Prefer a small `TestServiceProviderFactory` (or file-static helper) over per-test boilerplate.

## Risks / Trade-offs

- Overlapping with `fix-inmemory-configure` — apply options plumbing once; avoid conflicting PRs on the same files (serialize or one PR).
