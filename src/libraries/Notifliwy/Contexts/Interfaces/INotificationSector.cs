using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Models.Interfaces;

namespace Notifliwy.Contexts.Interfaces;

/// <summary>
/// Scoped <c>notification block</c>, contains assigned logic handler and <typeparamref name="TEvent"/> 
/// </summary>
public interface INotificationSector<in TEvent> where TEvent : IEvent
{
    /// <summary>
    /// Compilable method for handling events and notifications resulting from their events
    /// </summary>
    /// <param name="inputEvent">incoming event for all treatments</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns></returns>
    public ValueTask PassThroughAsync(
        TEvent inputEvent,
        CancellationToken cancellationToken = default);
}