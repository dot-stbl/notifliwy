using System;
using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Custom.Interfaces;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Join.Interfaces;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Transform.Interfaces;

namespace Notifliwy.Graph.Internals;

/// <summary>
/// Compiled counterpart of <see cref="SectorGraphPlan{TNotification,TEvent}"/>:
/// the same graph shape with node <b>instances</b> resolved or constructed once at
/// startup, so the hot path runs direct invokes with no per-event DI scope and no
/// per-event service resolution.
/// </summary>
/// <typeparam name="TNotification">The notification type produced by the <c>Map</c> node</typeparam>
/// <typeparam name="TEvent">The event type consumed by the sector</typeparam>
/// <param name="conditions">
///     Condition instances evaluated against the raw event, in registration order;
///     empty for branch sub-plans
/// </param>
/// <param name="map">
///     Compiled map invocation — either a wrapped mapper instance or the inline
///     mapping lambda of the plan. Always set for the top-level plan;
///     <see langword="null"/> for branch sub-plans
/// </param>
/// <param name="nodes">Compiled main-path nodes; for branch sub-plans this is the whole branch body</param>
internal sealed class CompiledSectorPlan<TNotification, TEvent>(
    INotificationCondition<TNotification, TEvent>[] conditions,
    Func<TEvent, CancellationToken, ValueTask<TNotification>>? map,
    CompiledNodeDefinition<TNotification, TEvent>[] nodes)
{
    /// <summary>
    /// Condition instances evaluated against the raw event, in registration order.
    /// Empty for branch sub-plans.
    /// </summary>
    public INotificationCondition<TNotification, TEvent>[] Conditions { get; } = conditions;

    /// <summary>
    /// Compiled map invocation — either a wrapped mapper instance or the inline
    /// mapping lambda of the plan. Always set for the top-level plan;
    /// <see langword="null"/> for branch sub-plans.
    /// </summary>
    public Func<TEvent, CancellationToken, ValueTask<TNotification>>? Map { get; } = map;

    /// <summary>
    /// Compiled main-path nodes. For branch sub-plans this is the whole branch body.
    /// </summary>
    public CompiledNodeDefinition<TNotification, TEvent>[] Nodes { get; } = nodes;
}

/// <summary>
/// Base of all compiled post-map node definitions.
/// </summary>
internal abstract class CompiledNodeDefinition<TNotification, TEvent>;

/// <summary>
/// Compiled <c>Transform</c> node holding the transform instance.
/// </summary>
internal sealed class CompiledTransformNode<TNotification, TEvent>(
    INotificationTransform<TNotification> transform) : CompiledNodeDefinition<TNotification, TEvent>
{
    /// <summary>
    /// Transform instance invoked on the hot path.
    /// </summary>
    public INotificationTransform<TNotification> Transform { get; } = transform;
}

/// <summary>
/// Compiled <c>Custom</c> node holding the invocation delegate — either a wrapped
/// custom service instance or the inline lambda of the plan.
/// </summary>
internal sealed class CompiledCustomNode<TNotification, TEvent>(
    Func<TNotification, CancellationToken, ValueTask<TNotification>> invocation)
        : CompiledNodeDefinition<TNotification, TEvent>
{
    /// <summary>
    /// Compiled custom invocation.
    /// </summary>
    public Func<TNotification, CancellationToken, ValueTask<TNotification>> Invocation { get; } = invocation;
}

/// <summary>
/// Compiled <c>Export</c> node holding the exporter instance.
/// </summary>
internal sealed class CompiledExportNode<TNotification, TEvent>(
    INotificationExporter<TNotification> exporter) : CompiledNodeDefinition<TNotification, TEvent>
{
    /// <summary>
    /// Exporter instance invoked on the hot path.
    /// </summary>
    public INotificationExporter<TNotification> Exporter { get; } = exporter;
}

/// <summary>
/// Compiled <c>Branch</c> node holding the compiled sub-plans of every branch.
/// </summary>
internal sealed class CompiledBranchNode<TNotification, TEvent>(
    BranchPolicy? policyOverride,
    CompiledSectorPlan<TNotification, TEvent>[] branchPlans) : CompiledNodeDefinition<TNotification, TEvent>
{
    /// <summary>
    /// Per-node policy override; <see langword="null"/> falls back to the sector default.
    /// </summary>
    public BranchPolicy? PolicyOverride { get; } = policyOverride;

    /// <summary>
    /// Compiled sub-plans of every branch, in registration order.
    /// </summary>
    public CompiledSectorPlan<TNotification, TEvent>[] BranchPlans { get; } = branchPlans;
}

/// <summary>
/// Compiled <c>Join</c> node holding the reducer instance.
/// </summary>
internal sealed class CompiledJoinNode<TNotification, TEvent>(
    INotificationJoin<TNotification> join) : CompiledNodeDefinition<TNotification, TEvent>
{
    /// <summary>
    /// Join reducer instance invoked on the hot path (single-branch joins skip it).
    /// </summary>
    public INotificationJoin<TNotification> Join { get; } = join;
}

/// <summary>
/// Compiled <c>When</c>/<c>Map</c> wrappers over resolved instances, kept as
/// delegate shapes for direct invocation on the hot path.
/// </summary>
internal static class CompiledNodeWrappers
{
    /// <summary>
    /// Wrap a mapper instance into the plan-level map delegate shape.
    /// </summary>
    public static Func<TEvent, CancellationToken, ValueTask<TNotification>> WrapMapper<TNotification, TEvent>(
        INotificationMapper<TNotification, TEvent> mapper)
    {
        return (inputEvent, cancellationToken) => mapper.ConvertAsync(inputEvent, cancellationToken);
    }

    /// <summary>
    /// Wrap a custom service instance into the node-level invocation delegate shape.
    /// </summary>
    public static Func<TNotification, CancellationToken, ValueTask<TNotification>> WrapCustom<TNotification, TEvent>(
        INotificationCustom<TNotification> custom)
    {
        return (notification, cancellationToken) => custom.InvokeAsync(notification, cancellationToken);
    }
}
