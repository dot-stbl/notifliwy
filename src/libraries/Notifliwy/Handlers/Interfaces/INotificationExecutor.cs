using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Connectors;
using Notifliwy.Models.Interfaces;

namespace Notifliwy.Handlers.Interfaces;

/// <summary>
/// Base handler executor from <see cref="NotificationConnectorService{TEvent}"/> context
/// </summary>
public interface INotificationExecutor<in TEvent>
    where TEvent : IEvent
{
    /// <summary>
    /// Starts a new independent task, using the configured layers
    /// </summary>
    public ValueTask StartAsync(
        TEvent inputEvent,
        CancellationToken cancellationToken = default);
}