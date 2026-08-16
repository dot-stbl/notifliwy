using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Mapper.Interfaces;

namespace Notifliwy.Transform.Interfaces;

/// <summary>
/// Defines a contract for transforming notifications in a pipeline.
/// Transforms are optional components that modify or enrich notifications
/// after they have been created by the <see cref="INotificationMapper{TNotification,TEvent}"/>.
/// Multiple transforms can be added to a graph and will be executed sequentially.
/// </summary>
/// <typeparam name="TNotification">The notification type to transform</typeparam>
/// <example>
/// Example transform that normalizes phone numbers:
/// <code>
/// public class NormalizePhoneNumberTransform : INotificationTransform&lt;MyNotification&gt;
/// {
///     public ValueTask&lt;MyNotification&gt; TransformAsync(MyNotification notification, CancellationToken cancellationToken = default)
///     {
///         notification.PhoneNumber = NormalizePhone(notification.PhoneNumber);
///         return ValueTask.FromResult(notification);
///     }
/// }
/// </code>
/// </example>
/// <remarks>
/// <para>Transforms are executed in the order they are added to the graph.
/// Each transform receives the notification from the previous transform (or the mapper)
/// and returns a modified or enriched version.</para>
/// <para>Transforms can be used to:</para>
/// <list type="bullet">
/// <item><description>Enrich notifications with additional data from external services</description></item>
/// <item><description>Apply business logic or validations</description></item>
/// <item><description>Transform notification format (e.g., change from DTO to domain model)</description></item>
/// </list>
/// <para>If a transform throws an exception, it will propagate up and
/// stop processing of the current path. Other branches of a fan-out
/// are governed by the sector <c>BranchPolicy</c>.</para>
/// <para>Renamed in 3.2 from <c>INotificationStep</c>/<c>AggregateAsync</c> —
/// <c>AggregateAsync</c> was a misnomer for a per-notification transform.</para>
/// </remarks>
public interface INotificationTransform<TNotification>
{
    /// <summary>
    /// Transforms or enriches the notification.
    /// </summary>
    /// <param name="notification">The notification to transform (output from previous transform or mapper)</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>The transformed notification, passed to the next transform or exporter</returns>
    /// <exception cref="System.Exception">May throw any exception during transformation</exception>
    /// <example>
    /// <code>
    /// var enrichedNotification = await transform.TransformAsync(notification);
    /// </code>
    /// </example>
    public ValueTask<TNotification> TransformAsync(
        TNotification notification,
        CancellationToken cancellationToken = default);
}
