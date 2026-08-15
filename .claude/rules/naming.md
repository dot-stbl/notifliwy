# C# Naming Conventions - Notifliwy Project

## General Rules
- Interfaces: `I` prefix (e.g., `INotificationMapper`, `IInputPipe`)
- No abbreviations: use `notification`, `condition`, not `notif`, `cond`
- Methods: verb phrases like `ConvertAsync`, `AllowItAsync`, `ThrowAsync`
- Private fields: NO underscore prefix, use auto-properties
- Always write access modifiers explicitly

## Private Fields

**NO underscore prefix.** Private fields MUST use auto-properties:

```csharp
// ✅ Correct - auto-property
public sealed class MyService
{
    public ILogger<MyService> Logger { get; }
}

// ✅ Correct - primary constructor with auto-property
public sealed class MyService(ILogger<MyService> logger)
{
    // logger is a primary constructor parameter
}

// ❌ Wrong - underscore prefix
private readonly string _someValue;
```

## Async Method Naming

All async methods MUST end with `Async`, CancellationToken as last parameter:

```csharp
// ✅ Correct
public async Task<Result> ProcessAsync(OrderRequest request, CancellationToken cancellationToken = default)
public ValueTask<Notification> ConvertAsync(Event input, CancellationToken cancellationToken = default)

// ❌ Wrong
public async Task<Result> Process(request)  // missing Async
public ValueTask<Notification> ConvertAsync(cancellationToken, input)  // CancellationToken not last
```

## Lambda Parameters

Use meaningful names, never single letters:

```csharp
// ✅ Correct
notifications.Where(notif => notif.IsActive)
             .Select(mapper => mapper.OutputType)
             .ForEach(exporter => exporter.ThrowAsync(notif, ct));

// ❌ Wrong
notifications.Where(n => n.IsActive)
             .Select(m => m.OutputType)
             .ForEach(e => e.ThrowAsync(n, ct));
```

## Parameter Naming

All method parameters MUST use descriptive names:

```csharp
// ✅ Correct
public async Task<Result> ProcessOrderAsync(OrderRequest orderRequest, CancellationToken cancellationToken = default)

// ❌ Wrong
public async Task<Result> ProcessOrderAsync(OrderRequest req, CancellationToken ct)
```

| Avoid | Use |
|-------|-----|
| `ct` | `cancellationToken` |
| `req` | `request` |
| `id` | `notificationId`, `eventId` |