using System.Threading;
using System.Threading.Tasks;

namespace Notifliwy.Custom.Interfaces;

/// <summary>
/// Defines a contract for custom escape-hatch nodes in the sector graph.
/// Custom nodes cover behaviour that does not fit <c>Transform</c> semantics
/// (e.g. rate limiting gates, reusable cross-sector policies) while keeping
/// the same notification-to-notification shape.
/// </summary>
/// <typeparam name="TNotification">The notification type to process</typeparam>
/// <example>
/// Example custom node that throttles processing:
/// <code>
/// public class RateLimitGate : INotificationCustom&lt;MyNotification&gt;
/// {
///     public ValueTask&lt;MyNotification&gt; InvokeAsync(MyNotification notification, CancellationToken cancellationToken = default)
///     {
///         return ValueTask.FromResult(notification);
///     }
/// }
/// </code>
/// </example>
/// <remarks>
/// <para>Custom nodes can be registered as DI classes (<c>Custom&lt;TCustom&gt;()</c>)
/// or as inline lambdas (<c>Custom((notification, cancellationToken) => …)</c>);
/// the lambda variant wraps into the same node shape internally.</para>
/// <para>A DI-registered custom class is shared per its service lifetime, so the
/// same class can be reused across sectors.</para>
/// </remarks>
public interface INotificationCustom<TNotification>
{
    /// <summary>
    /// Invokes the custom behaviour on the notification.
    /// </summary>
    /// <param name="notification">The notification coming from the previous node</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>The notification passed to the next node</returns>
    /// <exception cref="System.Exception">May throw any exception during invocation</exception>
    /// <example>
    /// <code>
    /// var gatedNotification = await custom.InvokeAsync(notification);
    /// </code>
    /// </example>
    public ValueTask<TNotification> InvokeAsync(
        TNotification notification,
        CancellationToken cancellationToken = default);
}
