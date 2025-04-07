using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Handlers.Interfaces;
using Notifliwy.Models.Interfaces;

namespace Notifliwy.Handlers;

/// <inheritdoc />
public class NotificationExecutor<TEvent>(INotificationHandler<TEvent>[] handlers) : INotificationExecutor<TEvent> 
    where TEvent : IEvent
{
    /// <inheritdoc />
    public ValueTask StartAsync(
        TEvent inputEvent, 
        CancellationToken cancellationToken = default)
    {
        foreach (var notificationHandler in handlers)
        {
            _ = Task.Run(
                function: async () => await notificationHandler.HandleAsync(
                    inputEvent,
                    cancellationToken), 
                cancellationToken);
        }

        return ValueTask.CompletedTask;
    }
}