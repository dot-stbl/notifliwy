using Notifliwy.Connectors;
using Notifliwy.Models.Interfaces;

namespace Notifliwy.Options;

/// <summary>
/// Base options of <see cref="NotificationConnectorService{TEvent}"/>
/// </summary>
/// <typeparam name="TEvent"></typeparam>
public class NotificationConnectorOptions<TEvent>
    where TEvent : IEvent
{
    /// <summary>
    /// Parallel worker handler for <typeparamref name="TEvent"/>
    /// </summary>
    public required int WorkerCount { get; init; } = 4;
}