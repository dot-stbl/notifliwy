using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Transform.Interfaces;

namespace Notifliwy.Mapper.Interfaces;

/// <summary>
/// Defines a contract for converting events to notifications.
/// The mapper is the required stage in every notification pipeline - all events
/// must be converted to a notification before they can be processed by
/// <see cref="INotificationTransform{TNotification}"/> transforms and
/// <see cref="INotificationExporter{TNotification}"/> exporters.
/// </summary>
/// <typeparam name="TNotification">The resulting notification type</typeparam>
/// <typeparam name="TEvent">The incoming event type to convert</typeparam>
/// <example>
/// Simple mapper that doubles a numeric value:
/// <code>
/// public class MultiplierMapper : INotificationMapper&lt;MyNotification, MyEvent&gt;
/// {
///     public ValueTask&lt;MyNotification&gt; ConvertAsync(MyEvent inputEvent, CancellationToken cancellationToken = default)
///     {
///         return ValueTask.FromResult(new MyNotification
///         {
///             Value = inputEvent.Value * 2
///         });
///     }
/// }
/// </code>
/// </example>
/// <remarks>
/// <para>The mapper is executed only if all registered <see cref="INotificationCondition{TNotification, TEvent}"/>
/// conditions return <see langword="true"/>. If any condition returns <see langword="false"/>,
/// the event is filtered out and the mapper is not called.</para>
/// <para>Implementations should be lightweight and fast, as they are called
/// for every event that passes through conditions.</para>
    /// <para>Complex transformations should be handled in <see cref="INotificationTransform{TNotification}"/>
    /// components rather than in the mapper.</para>
/// </remarks>
public interface INotificationMapper<TNotification, in TEvent>
{
    /// <summary>
    /// Converts the input event into a notification.
    /// </summary>
    /// <param name="inputEvent">The incoming event to convert</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task representing the converted notification</returns>
    /// <exception cref="System.Exception">May throw any exception during conversion</exception>
    /// <example>
    /// <code>
    /// var notification = await mapper.ConvertAsync(inputEvent);
    /// </code>
    /// </example>
    public ValueTask<TNotification> ConvertAsync(TEvent inputEvent, CancellationToken cancellationToken = default);
}