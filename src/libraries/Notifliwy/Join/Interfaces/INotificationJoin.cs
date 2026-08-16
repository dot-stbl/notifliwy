using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Notifliwy.Join.Interfaces;

/// <summary>
/// Defines a contract for reducing multiple branch outputs back into a single notification.
/// Joins are used after a <c>Branch</c> fan-out to merge the results of parallel
/// sub-graphs into the notification that continues down the main path.
/// </summary>
/// <typeparam name="TNotification">The notification type to reduce</typeparam>
/// <example>
/// Example join that merges branch outputs by summing their values:
/// <code>
/// public class SumJoin : INotificationJoin&lt;MyNotification&gt;
/// {
///     public ValueTask&lt;MyNotification&gt; JoinAsync(IReadOnlyList&lt;MyNotification&gt; notifications, CancellationToken cancellationToken = default)
///     {
///         return ValueTask.FromResult(new MyNotification
///         {
///             Value = notifications.Sum(notification => notification.Value)
///         });
///     }
/// }
/// </code>
/// </example>
/// <remarks>
/// <para>A join placed after a fan-out with a single surviving branch is a passthrough —
/// the executor returns the single output directly and does not invoke the reducer.</para>
/// <para>A multi-branch join without a reducer is a registration error in the 3.2
/// sector graph (<c>Join&lt;TJoin&gt;()</c> always carries the reducer type).</para>
/// </remarks>
public interface INotificationJoin<TNotification>
{
    /// <summary>
    /// Reduces the collected branch outputs into a single notification.
    /// </summary>
    /// <param name="notifications">Outputs of the joined branches, in branch registration order</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>The reduced notification, passed to the next node on the main path</returns>
    /// <exception cref="System.Exception">May throw any exception during reduction</exception>
    /// <example>
    /// <code>
    /// var reducedNotification = await join.JoinAsync(branchOutputs);
    /// </code>
    /// </example>
    public ValueTask<TNotification> JoinAsync(
        IReadOnlyList<TNotification> notifications,
        CancellationToken cancellationToken = default);
}
