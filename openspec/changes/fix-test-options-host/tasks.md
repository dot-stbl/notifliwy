## 1. Locate failures

- [ ] 1.1 Reproduce activation failure without `AddOptions`
- [ ] 1.2 List all tests that build bare `ServiceCollection` for in-memory

## 2. Fix host plumbing

- [ ] 2.1 Add shared test helper that registers options + Notifliwy pieces
- [ ] 2.2 Switch affected tests to the helper
- [ ] 2.3 (Optional) `AddOptions` inside library in-memory registration

## 3. Verify

- [ ] 3.1 `dotnet test test/Notifliwy.Units` green
- [ ] 3.2 Close GH #11
