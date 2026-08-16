## 1. Audit current API

- [x] 1.1 Map `AddInMemoryInput` overloads and what `configure` currently does
- [x] 1.2 Map how `InMemoryEventExchange` creates its channel

## 2. Options + wiring

- [x] 2.1 Introduce or fix options type for in-memory exchange
- [x] 2.2 Register options + apply configure callback
- [x] 2.3 Construct channel from options

## 3. Tests & docs

- [x] 3.1 Unit test: configure capacity (or equivalent) is observed
- [x] 3.2 Update README / NOTIFLIWY snippet if public configure is newly usable
- [x] 3.3 `dotnet test test/Notifliwy.Units`
- [x] 3.4 Close #8 and #4 when merged
