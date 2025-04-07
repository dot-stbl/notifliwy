using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Models.Interfaces;

namespace Notifliwy.Conditions;

/// <summary>
/// Derive default type from <see cref="INotificationConditionProcessor{TNotification,TEvent}"/>
/// </summary>
/// <inheritdoc />
internal class NotificationConditionProcessor<TNotification, TEvent> 
    : INotificationConditionProcessor<TNotification, TEvent>
        where TNotification : INotification
        where TEvent : IEvent
{
    /// <inheritdoc />
    public async ValueTask<bool> AllowConditionAsync(
        TEvent inputEvent, 
        INotificationCondition<TNotification, TEvent> condition,
        CancellationToken cancellationToken = default)
    {
        return await condition.AllowItAsync(inputEvent, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<bool> AllowConditionsAsync(
        TEvent inputEvent, 
        IReadOnlyCollection<INotificationCondition<TNotification, TEvent>> conditions,
        CancellationToken cancellationToken = default)
    {
        foreach (var notificationCondition in conditions)
        {
            if (!await notificationCondition.AllowItAsync(inputEvent, cancellationToken))
            {
                return false;
            }
        }
        
        return true;
    }
}