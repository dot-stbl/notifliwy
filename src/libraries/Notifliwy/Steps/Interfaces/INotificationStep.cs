using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Mapper.Interfaces;

namespace Notifliwy.Steps.Interfaces;

/// <summary>
/// Defines a contract for transforming notifications in a pipeline.
/// Steps are optional components that modify or enrich notifications
/// after they have been created by the <see cref="INotificationMapper{TNotification,TEvent}"/>.
/// Multiple steps can be added to a pipeline and will be executed sequentially.
/// </summary>
/// <typeparam name="TNotification">The notification type to transform</typeparam>
/// <example>
/// Example step that normalizes phone numbers:
/// <code>
/// public class NormalizePhoneNumberStep : INotificationStep&lt;MyNotification&gt;
/// {
///     public ValueTask&lt;MyNotification&gt; AggregateAsync(MyNotification notification, CancellationToken cancellationToken = default)
///     {
///         notification.PhoneNumber = NormalizePhone(notification.PhoneNumber);
///         return ValueTask.FromResult(notification);
///     }
/// }
/// </code>
/// </example>
/// <remarks>
/// <para>Steps are executed in the order they are added to the pipeline.
/// Each step receives the notification from the previous step (or the mapper)
/// and returns a modified or enriched version.</para>
/// <para>Steps can be used to:</para>
/// <list type="bullet">
/// <item><description>Enrich notifications with additional data from external services</description></item>
/// <item><description>Apply business logic or validations</description></item>
/// <item><description>Transform notification format (e.g., change from DTO to domain model)</description></item>
/// <item><description>Aggregate data from multiple sources</description></item>
/// </list>
/// <para>If a step throws an exception, it will propagate up and
/// stop processing of the current pipeline. Other pipelines in the sector
/// are not affected.</para>
/// </remarks>
public interface INotificationStep<TNotification>
{
    /// <summary>
    /// Transforms or enriches the notification.
    /// </summary>
    /// <param name="notification">The notification to transform (output from previous step or mapper)</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>The transformed notification, passed to the next step or exporter</returns>
    /// <exception cref="System.Exception">May throw any exception during transformation</exception>
    /// <example>
    /// <code>
    /// var enrichedNotification = await step.AggregateAsync(notification);
    /// </code>
    /// </example>
    public ValueTask<TNotification> AggregateAsync(
        TNotification notification,
        CancellationToken cancellationToken = default);
}