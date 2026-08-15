# C# Code Patterns - Notifliwy Project

## Primary Constructors (Always Use)

### Parameter Sorting (Pyramid Rule)
Parameters sorted by length (shortest to longest):

```csharp
// ✅ Correct - pyramid layout
public sealed class NotificationSectorBuilder(
    IServiceCollection serviceCollection)

// ❌ Wrong - unsorted
public sealed class NotificationSectorBuilder(
    ILogger<NotificationSectorBuilder> logger,
    IServiceCollection serviceCollection)
```

### Basic Usage
```csharp
// ✅ Correct - primary constructor
public sealed class SectorBlock<TNotification, TEvent>(
    IServiceProvider serviceProvider,
    ILogger<SectorBlock<TNotification, TEvent>> sectorLogger,
    INotificationConditionProcessor<TNotification, TEvent> conditionProcessor)

// ❌ Wrong - old style
public class SectorBlock<TNotification, TEvent>
{
    private readonly IServiceProvider _serviceProvider;
    public SectorBlock(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;
}
```

## Pattern Matching

### Null Checks
```csharp
// ✅ Assign and check in one expression
if (await repository.GetByIdAsync(id, cancellationToken) is { } entity)
{
    // entity is not null
}

// ✅ Check for null
if (result is not {} notification)
{
    return ValueTask.FromResult(false);
}
```

## Records for Immutable Data
```csharp
public sealed record DiagnosticEventData<TEvent>
{
    public static ActivityTagsCollection TagsBy { get; }
}
```

## No Private Methods

**Never create private methods.** Instead:
- Use `file static class` with extension methods
- Use local functions inside the method

```csharp
// ✅ Extension method
file static class NotificationExtensions
{
    public static bool IsValid(this Notification notification) => notification.Exporters.Count > 0;
}

// ✅ Local function inside method
public async ValueTask ProcessAsync(Event input, CancellationToken cancellationToken)
{
    var validate = () => input.Value > 0;
    if (!validate()) return;
    // ...
}
```

## Async/Await Rules
- All I/O operations: async/await
- NO `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`
- Use `ValueTask` for synchronous-heavy async operations
- Suffix `Async` for all async methods

## Condition Processing Pattern

```csharp
public async ValueTask<bool> AllowConditionAsync(
    TEvent inputEvent,
    INotificationCondition<TNotification, TEvent> condition,
    CancellationToken cancellationToken = default)
{
    return await condition.AllowItAsync(inputEvent, cancellationToken);
}
```