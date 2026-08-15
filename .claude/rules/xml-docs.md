# XML Documentation Rules - Notifliwy Project

## Where to Add Summary Blocks

### Always Add `<summary>` To:
- **Interfaces**: all members (INotificationCondition, INotificationMapper, etc.)
- **Base classes/records**: all public members

### Use `<inheritdoc/>` For:
- **Derived implementations**: inherited members

### No Inline Comments
- **NEVER use `//` or `/* */` comments inside code blocks**
- **Only `/// <summary>...</summary>` XML documentation blocks are allowed**
- Document intent via well-named identifiers and XML docs

### Private Members
- **Always add explicit `<summary>`** for private fields and methods

## Example

```csharp
/// <summary>
/// Condition processor for notification filtering.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
/// <typeparam name="TEvent">The event type.</typeparam>
internal class NotificationConditionProcessor<TNotification, TEvent>
    : INotificationConditionProcessor<TNotification, TEvent>
{
    /// <summary>
    /// Processes single condition check.
    /// </summary>
    public async ValueTask<bool> AllowConditionAsync(
        TEvent inputEvent,
        INotificationCondition<TNotification, TEvent> condition,
        CancellationToken cancellationToken = default)
    {
        return await condition.AllowItAsync(inputEvent, cancellationToken);
    }
}
```