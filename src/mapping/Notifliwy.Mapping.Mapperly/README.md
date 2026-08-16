# Notifliwy.Mapping.Mapperly

Source-generated mapping provider for **Notifliwy** — the blessed default for compile-time
event-to-notification mapping via [Mapperly](https://mapperly.riok.app/).

The adapter package wraps a Mapperly-generated mapper into the Notifliwy
`INotificationMapper<TNotification, TEvent>` contract, so a sector graph `Map` node
consumes a compile-time generated mapping without any hand-written adapter code.

Referencing this package transitively brings the Mapperly source generator, no extra
`Riok.Mapperly` reference is required.

## Worked example

Event and notification:

```csharp
public sealed class CatMeowEvent
{
    public int Volume { get; init; }
}

public sealed class CatMeowNotification
{
    public int Volume { get; set; }
}
```

Declare a mapping contract and let Mapperly implement it:

```csharp
using Notifliwy.Mapping.Mapperly;
using Riok.Mapperly.Abstractions;

public interface ICatMeowMapping : IMapperlyNotificationMapping<CatMeowNotification, CatMeowEvent>;

[Mapper]
public sealed partial class CatMeowMapper : ICatMeowMapping
{
    public partial CatMeowNotification ToNotification(CatMeowEvent inputEvent);
}
```

Plug it into a sector graph `Map` node — either by registration:

```csharp
services.AddNotifliwyMapperlyMapping<CatMeowNotification, CatMeowEvent, CatMeowMapper>();

graph.Map<MapperlyNotificationMapper<CatMeowNotification, CatMeowEvent, CatMeowMapper>>();
```

or by a one-line subclass that pins the closed generics to a short name:

```csharp
public sealed class CatMeowNotificationMapper()
    : MapperlyNotificationMapper<CatMeowNotification, CatMeowEvent, CatMeowMapper>(new CatMeowMapper());

graph.Map<CatMeowNotificationMapper>();
```

The generated mapper is stateless and thread-safe; register the adapter as a singleton
(`AddNotifliwyMapperlyMapping` does this for you).

## When to use: Mapperly vs Mapster vs inline lambda

| Shape | Use when |
|-------|----------|
| **Mapperly** (this package) | Default choice. Compile-time generated, zero reflection at runtime, mapping errors are build errors. |
| **Mapster** (`Notifliwy.Mapping.Mapster`) | You already own `TypeAdapterConfig` rules or need runtime-configured mapping. |
| Inline lambda `Map((ev, ct) => ...)` | One-off trivial projection local to a single sector; no reuse, no test surface of its own. |

Switching providers does not change the graph — `Map<TAdapter>()` accepts any
`INotificationMapper<TNotification, TEvent>` implementation, so a Mapperly-backed
mapper and a Mapster-backed mapper are interchangeable.

## Project

This project comes with an [GNU3.0](../../../LICENSE). Contact the `.stbl` group.
