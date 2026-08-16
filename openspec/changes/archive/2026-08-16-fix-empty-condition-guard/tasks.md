## 1. Reproduce

- [x] 1.1 Confirm failing path: sector with mapper only → event → `EmptyInstanceBranchException` (or silent drop) in current code
- [x] 1.2 Add failing unit/integration test for mapper-only sector that expects exporter invocation

## 2. Fix

- [x] 2.1 Guard condition stage in `SectorBlock` when `!ConditionInstances.UseInstance` (allow)
- [x] 2.2 Ensure multi-target build still compiles (`net6.0;net7.0;net8.0`)

## 3. Verify

- [x] 3.1 `dotnet test test/Notifliwy.Units` passes
- [x] 3.2 Comment on / close GH #7 after merge
