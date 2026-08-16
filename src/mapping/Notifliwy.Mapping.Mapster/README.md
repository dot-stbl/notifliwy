# Notifliwy.Mapping.Mapster

Runtime mapping provider for **Notifliwy** via [Mapster](https://github.com/MapsterMapper/Mapster),
for consumers that already own `TypeAdapterConfig` rules. For new code prefer the blessed
compile-time default: `Notifliwy.Mapping.Mapperly`.

The adapter compiles the `<TEvent> → <TNotification>` rule into a delegate once
(`TypeAdapterConfig.GetMapFunction`) and serves every conversion from it, wrapped into
the Notifliwy `INotificationMapper<TNotification, TEvent>` contract a sector graph
`Map` node consumes.

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

Register the Mapster rule and the adapter:

```csharp
using Mapster;
using Notifliwy.Mapping.Mapster;

services.AddNotifliwyMapsterMapping(configure: config =>
    config.NewConfig<CatMeowEvent, CatMeowNotification>());
services.AddNotifliwyMapsterMapping<CatMeowNotification, CatMeowEvent>();

graph.Map<MapsterNotificationMapper<CatMeowNotification, CatMeowEvent>>();
```

Alternatively construct the adapter directly — from an explicit config, from the global
settings (the `.AdaptToType<TDestination>()` world), or from any pre-compiled
`Func<TEvent, TNotification>`:

```csharp
INotificationMapper<CatMeowNotification, CatMeowEvent> mapper =
    new MapsterNotificationMapper<CatMeowNotification, CatMeowEvent>(config);
```

Compiled Mapster delegates are thread-safe; register the adapter as a singleton
(`AddNotifliwyMapsterMapping` does this for you).

## When to use: Mapster vs Mapperly vs inline lambda

| Shape | Use when |
|-------|----------|
| **Mapperly** (`Notifliwy.Mapping.Mapperly`) | Default choice. Compile-time generated, zero reflection at runtime, mapping errors are build errors. |
| **Mapster** (this package) | You already own `TypeAdapterConfig` rules or need runtime-configured mapping. |
| Inline lambda `Map((ev, ct) => ...)` | One-off trivial projection local to a single sector; no reuse, no test surface of its own. |

Switching providers does not change the graph — `Map<TAdapter>()` accepts any
`INotificationMapper<TNotification, TEvent>` implementation, so a Mapster-backed
mapper and a Mapperly-backed mapper are interchangeable.

## Project

This project comes with an [GNU3.0](../../../LICENSE). Contact the `.stbl` group.
