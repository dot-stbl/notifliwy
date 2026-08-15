using System.Threading;
using System.Threading.Tasks;

namespace Notifliwy.Conditions.Interfaces;

/// <summary>
/// Defines a contract for conditional event processing.
/// Conditions allow filtering of events before they proceed to the
/// mapper stage. If a condition returns <see langword="false"/>, the event
/// is skipped and no further processing occurs.
/// </summary>
/// <typeparam name="TNotification">The notification type that will be created if condition passes</typeparam>
/// <typeparam name="TEvent">The input event type to evaluate</typeparam>
/// <example>
/// Example condition that only processes even-numbered events:
/// <code>
/// public class EvenNumberCondition : INotificationCondition&lt;MyNotification, MyEvent&gt;
/// {
///     public ValueTask&lt;bool&gt; AllowItAsync(MyEvent inputEvent, CancellationToken cancellationToken = default)
///     {
///         return ValueTask.FromResult(inputEvent.Value % 2 == 0);
///     }
/// }
/// </code>
/// </example>
/// <remarks>
/// <para>Conditions are evaluated before the <see cref="INotificationMapper{TNotification, TEvent}"/> stage.
/// Multiple conditions can be registered; all must return <see langword="true"/>
/// for the event to proceed.</para>
/// <para>If any condition returns <see langword="false"/>, the event is filtered out
/// and no further processing occurs. The <see cref="INotificationMapper"/> and
/// <see cref="INotificationExporter{TNotification}"/> stages are not called.</para>
/// </remarks>
public interface INotificationCondition<TNotification, in TEvent>
{
    /// <summary>
    /// Determines whether the input event should be processed.
    /// </summary>
    /// <param name="inputEvent">The incoming event to evaluate</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>
    /// <see langword="true"/> if the event should proceed to the mapper stage;
    /// <see langword="false"/> if the event should be filtered out
    /// </returns>
    /// <example>
    /// <code>
    /// var shouldProcess = await condition.AllowItAsync(inputEvent);
    /// if (shouldProcess)
    /// {
    ///     var notification = await mapper.ConvertAsync(inputEvent);
    ///     await exporter.ThrowAsync(notification);
    /// }
    /// </code>
    /// </example>
    /// <exception cref="System.Exception">May throw any exception during evaluation</exception>
    ValueTask<bool> AllowItAsync(TEvent inputEvent, CancellationToken cancellationToken = default);
}