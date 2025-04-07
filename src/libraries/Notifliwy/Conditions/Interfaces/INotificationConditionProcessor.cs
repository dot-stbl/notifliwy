using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Notifliwy.Models.Interfaces;

namespace Notifliwy.Conditions.Interfaces;

/// <summary>
/// The main contractor of checking incoming events for passing conditions
/// </summary>
/// <typeparam name="TNotification">assigned notification type</typeparam>
/// <typeparam name="TEvent">assigned event type</typeparam>
public interface INotificationConditionProcessor<TNotification, TEvent>
    where TNotification : INotification
    where TEvent : IEvent
{
    /// <summary>
    /// Allow <paramref name="inputEvent"/> from <paramref name="condition"/>
    /// </summary>
    /// <returns>all condition is <c>allow</c></returns>
    public ValueTask<bool> AllowConditionAsync(
        TEvent inputEvent,
        INotificationCondition<TNotification, TEvent> condition,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Allow <paramref name="inputEvent"/> from <paramref name="conditions"/>
    /// </summary>
    /// <returns>all condition is <c>allow</c></returns>
    public ValueTask<bool> AllowConditionsAsync(
        TEvent inputEvent,
        IReadOnlyCollection<INotificationCondition<TNotification, TEvent>> conditions,
        CancellationToken cancellationToken = default);
}