using System.Threading;
using System.Threading.Tasks;

namespace Notifliwy.Steps.Interfaces;

/// <summary>
/// Base interface as step builder logic
/// </summary>
/// <typeparam name="TNotification">assigned notification type</typeparam>
public interface INotificationStep<TNotification>
{
    /// <summary>
    /// Aggregate <paramref name="notification"/> in steps pipeline
    /// </summary>
    /// <returns>aggregated notification</returns>
    public ValueTask<TNotification> AggregateAsync(
        TNotification notification,
        CancellationToken cancellationToken = default);
}