using System.Threading;
using System.Threading.Tasks;

namespace Notifliwy.Exporters.Interfaces;

/// <summary>
/// Defines a contract for exporting notifications to external systems.
/// Exporters are responsible for delivering processed notifications to their final destination
/// (e.g., database, message queue, HTTP API, email service, etc.).
/// </summary>
/// <typeparam name="TNotification">Type of notification to export</typeparam>
/// <example>
/// Example exporter that sends notifications to a message queue:
/// <code>
/// public class QueueExporter : INotificationExporter&lt;MyNotification&gt;
/// {
///     public ValueTask ThrowAsync(MyNotification notification, CancellationToken cancellationToken = default)
///     {
///         await queueClient.SendMessageAsync(notification);
///         return ValueTask.CompletedTask;
///     }
/// }
/// </code>
/// </example>
/// <remarks>
/// <para>Implementations should handle errors gracefully. If an exception is thrown,
/// it will be logged by the notification pipeline but will not prevent other
/// exporters from being called (unless in a pipeline without multiple exporters).</para>
/// </remarks>
public interface INotificationExporter<in TNotification>
{
    /// <summary>
    /// Export the specified notification to its destination.
    /// </summary>
    /// <param name="notification">The notification to export</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task representing the asynchronous export operation</returns>
    /// <exception cref="System.Exception">May throw any exception related to the export operation</exception>
    /// <example>
    /// <code>
    /// await exporter.ThrowAsync(notification, cancellationToken);
    /// </code>
    /// </example>
    ValueTask ThrowAsync(TNotification notification, CancellationToken cancellationToken = default);
}