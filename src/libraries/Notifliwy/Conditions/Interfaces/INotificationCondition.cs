using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Models.Interfaces;

namespace Notifliwy.Conditions.Interfaces;

/// <summary>
/// Custom event condition handler
/// </summary>
/// <typeparam name="TNotification">assigned notification type</typeparam>
/// <typeparam name="TEvent">assigned event type</typeparam>
public interface INotificationCondition<TNotification, in TEvent>
    where TEvent : IEvent
    where TNotification : INotification
{
    /// <summary>
    /// Internal check if the event can go further down the pipeline 
    /// </summary>
    public ValueTask<bool> AllowItAsync(TEvent inputEvent, CancellationToken cancellationToken = default);
}