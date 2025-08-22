using System.Threading;
using System.Threading.Tasks;

namespace Notifliwy.Mapper.Interfaces;

/// <summary>
/// Base cast service by <typeparamref name="TNotification"/> and <typeparamref name="TEvent"/>
/// </summary>
public interface INotificationMapper<TNotification, in TEvent>
{
    /// <summary>
    /// Cast <paramref name="inputEvent"/> to <typeparamref name="TNotification"/>
    /// </summary>
    public ValueTask<TNotification> ConvertAsync(TEvent inputEvent, CancellationToken cancellationToken = default);
}