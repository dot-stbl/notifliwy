using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Models.Interfaces;

namespace Notifliwy.Handlers.Interfaces;

/// <summary>
/// Base handler of <typeparamref name="TEvent"/> and assigned <c>Notification</c>
/// </summary>
/// <typeparam name="TEvent"></typeparam>
public interface INotificationHandler<in TEvent>
    where TEvent : IEvent
{
    /// <summary>
    /// Handle <typeparamref name="TEvent"/>
    /// </summary>
    public ValueTask HandleAsync(TEvent inputEvent, CancellationToken cancellationToken = default);
}
    