using System.Threading;
using Notifliwy.Exceptions;
using System.Threading.Tasks;
using Notifliwy.Models.Interfaces;
using Notifliwy.Handlers.Interfaces;
using Notifliwy.Contexts.Interfaces;

namespace Notifliwy.Handlers;

/// <summary>
/// Base <see cref="INotificationHandler{TEvent}"/> derive type, invoke event processing operation
/// </summary>
public class NotificationHandler<TNotification, TEvent>(
    INotificationSector<TNotification, TEvent> notificationSector) : INotificationHandler<TEvent>
        where TNotification : INotification
        where TEvent : IEvent
{
    /// <summary>
    /// Main method, handle <paramref name="inputEvent"/> and compute to final notification
    /// </summary>
    /// <exception cref="EventTransactionException">if an unexplained error occurred during event processing</exception>
    public async ValueTask HandleAsync(TEvent inputEvent, CancellationToken cancellationToken = default)
    {
        if (await notificationSector.CompiledCondition(inputEvent, cancellationToken) is false)
        {
            return;
        }
        
        var mappedNotification = await notificationSector
            .CompiledPipeline(
                await notificationSector.CompiledMapper(inputEvent, cancellationToken), 
                cancellationToken);
        
        await notificationSector.CompiledExporter(mappedNotification, cancellationToken);
    }
}