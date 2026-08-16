# Notifliwy — Found Issues

## Bug 1: MultiplyServiceInstance.CheckoutInstanceAsync — Always calls multiplyAction after singleAction

**File:** `src/libraries/Notifliwy/Related/MultiplyServiceInstance.cs`, lines 117-132

**Severity:** High

**Status:** FIXED ✓

**Description:**
After executing `singleAction` for a single instance, the code falls through and also calls `multiplyAction` with the `Multiply` array. This is incorrect — when `IsSingle` is true, only `singleAction` should execute.

**Fix applied:** Added `else` block so `multiplyAction` only executes when `IsSingle` is false.

---

## Bug 2: Fire-and-forget Task.Run in NotificationConnector — Silent Exception Swallowing

**File:** `src/libraries/Notifliwy/Connectors/NotificationConnector.cs`, line 48

**Severity:** Medium

**Status:** DOCUMENTED (not a bug - by design)

**Description:**
`Task.Run` is used without `await`. Exceptions in sector processing are silently swallowed.

**Resolution:**
Fire-and-forget is intentional here. Sector errors are caught and logged inside `SectorBlock.ProcessingAsync`. Added clarifying comment explaining this design decision.

**Comment added:**
```csharp
// Fire-and-forget: sector errors are caught and logged inside SectorBlock.ProcessingAsync
_ = Task.Run(() => sector.PassThroughAsync(handledEvent, token), token);
```

---

## Bug 3: Potential Duplicate ConnectorsBuilder for Same TEvent

**File:** `src/libraries/Notifliwy/Builders/NotificationServerBuilder.cs`, line 49

**Severity:** Low

**Status:** NOT A BUG (by design)

**Description:**
For each `AddNotification<TNotification, TEvent>` call, a new `ConnectorsBuilder<TEvent>` is added. `HashSet<IConnectorBuilder>` uses default equality, so instances are not deduplicated by `TEvent`.

**Resolution:**
Verified as intentional — multiple sectors can share the same `TEvent`, each creating its own connector. This is the expected multi-sector processing model.

---

## Bug 4: EnumerableExtensions.AggregateAsync uses Task instead of ValueTask

**File:** `src/libraries/Notifliwy/Extensions/EnumerableExtensions.cs`, line 19

**Severity:** Medium

**Status:** FIXED ✓

**Description:**
The `AggregateAsync` extension method used `Func<TAccumulate, TSource, Task<TAccumulate>>` instead of `Func<TAccumulate, TSource, ValueTask<TAccumulate>>`. Notifliwy consistently uses `ValueTask` for async operations to avoid allocations.

**Fix applied:**
- Changed parameter type from `Task<TAccumulate>` to `ValueTask<TAccumulate>`
- Simplified `NotificationPipeline` lambda to return `ValueTask` directly without `async/await` wrapper

---

## Bug 8: SectorBlock.ProcessingAsync throws on empty condition set

**File:** `src/libraries/Notifliwy/Contexts/SectorBlock.cs`

**Severity:** High

**Status:** FIXED ✓

**Description:**
`ProcessingAsync` called `ConditionInstances.CheckoutInstanceAsync` without the `UseInstance` guard. A sector registered without conditions (`AddCondition` is optional) threw `EmptyInstanceBranchException` on every event; `NotificationSector` swallowed it, so the sector stayed quiet and exported nothing. Pipelines and exporters already had the same guard — only conditions were missing it.

**Fix applied:**
Guarded the condition check with `ConditionInstances.UseInstance` — an empty condition set now means "allow all", matching the documented optional-condition semantics.

---

## Not Issues (Analyzed)

| Item | Reason |
|------|--------|
| #5 SectorBlock MultiplyServiceInstance lifetime | Not a bug — DI registration happens at startup, no new services appear mid-stream |
| #6 Channel capacity 1,000,000 | Design choice — unbounded wait mode is intentional for reliability |
| #7 DiagnosticMeter instance creation | Not an issue — class is `static`, single instance always exists |

---

## Summary

| Bug | Status |
|-----|--------|
| #1 MultiplyServiceInstance.CheckoutInstanceAsync | FIXED |
| #2 Fire-and-forget Task.Run | DOCUMENTED (by design) |
| #3 Duplicate ConnectorsBuilder | NOT A BUG (by design) |
| #4 AggregateAsync Task vs ValueTask | OPEN |
| #8 Empty condition set throws | FIXED |

**Next action:** Fix Bug #4 (AggregateAsync ValueTask) — high priority due to performance impact.