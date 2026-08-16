using System;
using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Custom.Interfaces;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Join.Interfaces;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Transform.Interfaces;

namespace Notifliwy.Graph.Interfaces;

/// <summary>
/// Fluent builder describing a typed sector graph (G2):
/// <c>When* → Map → (Transform | Branch | Join | Custom | Export)*</c>.
/// </summary>
/// <typeparam name="TNotification">The notification type produced by the <c>Map</c> node</typeparam>
/// <typeparam name="TEvent">The event type consumed by the sector</typeparam>
/// <example>
/// <code>
/// graphBuilder
///     .When&lt;OrderCreatedCondition&gt;()
///     .Map((orderEvent, cancellationToken) => ValueTask.FromResult(new OrderNotification(orderEvent.Id)))
///     .Transform&lt;EnrichmentTransform&gt;()
///     .Branch(
///         branch => branch.Transform&lt;EmailBodyTransform&gt;().Export&lt;EmailExporter&gt;(),
///         branch => branch.Transform&lt;SlackBodyTransform&gt;().Export&lt;SlackExporter&gt;())
///     .Join&lt;NotificationMergeJoin&gt;()
///     .Export&lt;AuditExporter&gt;();
/// </code>
/// </example>
/// <remarks>
/// <para>The graph is acyclic by construction: nodes are registered in a linear
/// main path and <c>Branch</c> nodes hold sub-graphs built from the same builder
/// shape, so a cycle cannot be expressed. Structure is validated when the plan is
/// built: <c>Map</c> must be registered exactly once and before any other node,
/// <c>Join</c> is only valid after a <c>Branch</c>, and every branch sub-graph must
/// terminate with at least one <c>Export</c> node.</para>
/// <para>Conditions (<c>When</c>) operate on the raw event and must be registered
/// before <c>Map</c>.</para>
/// </remarks>
public interface ISectorGraphBuilder<TNotification, TEvent>
{
    /// <summary>
    /// Adds a condition evaluated against the raw event before mapping.
    /// All registered conditions must allow the event for processing to continue.
    /// Must be registered before <see cref="Map{TMapper}"/>.
    /// </summary>
    /// <typeparam name="TCondition">custom condition handler</typeparam>
    ISectorGraphBuilder<TNotification, TEvent> When<TCondition>()
            where TCondition : class, INotificationCondition<TNotification, TEvent>;

    /// <summary>
    /// Sets the mapper converting the event into the notification, by DI-registered class.
    /// Required exactly once per sector graph.
    /// </summary>
    /// <typeparam name="TMapper">custom mapper for <c>event</c> to <c>notification</c></typeparam>
    ISectorGraphBuilder<TNotification, TEvent> Map<TMapper>()
            where TMapper : class, INotificationMapper<TNotification, TEvent>;

    /// <summary>
    /// Sets the mapper converting the event into the notification, by inline lambda.
    /// Required exactly once per sector graph.
    /// </summary>
    /// <param name="mapping">inline conversion from event to notification</param>
    ISectorGraphBuilder<TNotification, TEvent> Map(
        Func<TEvent, CancellationToken, ValueTask<TNotification>> mapping);

    /// <summary>
    /// Adds a transform node: notification to notification via
    /// <see cref="INotificationTransform{TNotification}.TransformAsync"/>.
    /// Multiple transforms on one path run sequentially, each receiving the previous output.
    /// </summary>
    /// <typeparam name="TTransform">custom transform handler</typeparam>
    ISectorGraphBuilder<TNotification, TEvent> Transform<TTransform>()
            where TTransform : class, INotificationTransform<TNotification>;

    /// <summary>
    /// Adds an export node delivering the notification at the current position of the path.
    /// </summary>
    /// <typeparam name="TExporter">custom exporter</typeparam>
    ISectorGraphBuilder<TNotification, TEvent> Export<TExporter>()
            where TExporter : class, INotificationExporter<TNotification>;

    /// <summary>
    /// Adds a branch fan-out: every branch sub-graph runs in parallel over the same
    /// input notification with the default <see cref="BranchPolicy"/> ( FailFast ).
    /// </summary>
    /// <param name="branches">branch sub-graph configurations</param>
    ISectorGraphBuilder<TNotification, TEvent> Branch(
        params Action<ISectorGraphBuilder<TNotification, TEvent>>[] branches);

    /// <summary>
    /// Adds a branch fan-out with an explicit <paramref name="policy"/> override
    /// for this single <c>Branch</c> node.
    /// </summary>
    /// <param name="policy">failure policy applied to this fan-out</param>
    /// <param name="branches">branch sub-graph configurations</param>
    ISectorGraphBuilder<TNotification, TEvent> Branch(
        BranchPolicy policy,
        params Action<ISectorGraphBuilder<TNotification, TEvent>>[] branches);

    /// <summary>
    /// Adds a join node reducing the outputs of the preceding <c>Branch</c> fan-out
    /// back into a single notification. A single-branch join is a passthrough and
    /// does not invoke the reducer. Only valid after a <c>Branch</c>.
    /// </summary>
    /// <typeparam name="TJoin">custom join reducer</typeparam>
    ISectorGraphBuilder<TNotification, TEvent> Join<TJoin>()
            where TJoin : class, INotificationJoin<TNotification>;

    /// <summary>
    /// Adds a custom escape-hatch node implemented by a DI-registered class.
    /// </summary>
    /// <typeparam name="TCustom">custom invocation handler</typeparam>
    ISectorGraphBuilder<TNotification, TEvent> Custom<TCustom>()
            where TCustom : class, INotificationCustom<TNotification>;

    /// <summary>
    /// Adds a custom escape-hatch node implemented by an inline lambda,
    /// wrapped into the same node shape internally.
    /// </summary>
    /// <param name="invocation">inline notification to notification invocation</param>
    ISectorGraphBuilder<TNotification, TEvent> Custom(
        Func<TNotification, CancellationToken, ValueTask<TNotification>> invocation);
}
