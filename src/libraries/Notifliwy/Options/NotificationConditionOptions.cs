using Notifliwy.Conditions.Interfaces;
using Notifliwy.Models.Interfaces;

namespace Notifliwy.Options;

/// <summary>
/// <c>Notification condition processor</c> options
/// </summary>
/// <typeparam name="TNotification">Assigned notificaiton type</typeparam>
/// <typeparam name="TEvent">Assigned event type</typeparam>
public class NotificationConditionOptions<TNotification, TEvent>
    where TNotification : INotification
    where TEvent : IEvent
{
    /// <summary>
    /// Use <see cref="INotificationCondition{TNotification,TEvent}"/> or not
    /// </summary>
    public bool UseConditions { get; init; } = false;

    /// <summary>
    /// Use only single <see cref="INotificationCondition{TNotification,TEvent}"/>
    /// </summary>
    public bool UseSingleCondition { get; init; } = false;

    /// <summary>
    /// Use collection of <see cref="INotificationCondition{TNotification,TEvent}"/>
    /// </summary>
    public bool UseMultiplyConditions { get; init; } = false;
}