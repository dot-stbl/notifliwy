## 1. Audit current API

- [ ] 1.1 Map `AddInMemoryInput` overloads and what `configure` currently does
- [ ] 1.2 Map how `InMemoryEventExchange` creates its channel

## 2. Options + wiring

- [ ] 2.1 Introduce or fix options type for in-memory exchange
- [ ] 2.2 Register options + apply configure callback
- [ ] 2.3 Construct channel from options

## 3. Tests & docs

- [ ] 3.1 Unit test: configure capacity (or equivalent) is observed
- [ ] 3.2 Update README / NOTIFLIWY snippet if public configure is newly usable
- [ ] 3.3 `dotnet test test/Notifliwy.Units`
- [ ] 3.4 Close #8 and #4 when merged
