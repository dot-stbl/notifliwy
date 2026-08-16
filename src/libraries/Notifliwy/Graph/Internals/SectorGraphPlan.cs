using System;
using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Config;

namespace Notifliwy.Graph.Internals;

/// <summary>
/// Single registration entry recorded by <see cref="SectorGraphBuilder{TNotification,TEvent}"/>
/// in call order; validated and frozen into <see cref="SectorGraphPlan{TNotification,TEvent}"/>.
/// </summary>
internal abstract class GraphRegistration;

/// <summary>
/// Recorded <c>When</c> node: a condition type evaluated against the raw event.
/// </summary>
internal sealed class GraphWhenRegistration(Type conditionType) : GraphRegistration
{
    /// <summary>
    /// Registered <see cref="Conditions.Interfaces.INotificationCondition{TNotification,TEvent}"/> type
    /// </summary>
    public Type ConditionType { get; } = conditionType;
}

/// <summary>
/// Recorded <c>Map</c> node: either a mapper service type or an inline lambda.
/// </summary>
internal sealed class GraphMapRegistration<TNotification, TEvent>(
    Type? mapperType,
    Func<TEvent, CancellationToken, ValueTask<TNotification>>? mapping) : GraphRegistration
{
    /// <summary>
    /// Registered <see cref="Mapper.Interfaces.INotificationMapper{TNotification,TEvent}"/> type, when class-based
    /// </summary>
    public Type? MapperType { get; } = mapperType;

    /// <summary>
    /// Inline conversion lambda, when lambda-based
    /// </summary>
    public Func<TEvent, CancellationToken, ValueTask<TNotification>>? Mapping { get; } = mapping;
}

/// <summary>
/// Base of all post-map node registrations.
/// </summary>
internal abstract class GraphNodeDefinition<TNotification, TEvent> : GraphRegistration;

/// <summary>
/// Recorded <c>Transform</c> node.
/// </summary>
internal sealed class GraphTransformDefinition<TNotification, TEvent>(Type transformType)
        : GraphNodeDefinition<TNotification, TEvent>
{
    /// <summary>
    /// Registered <see cref="Transform.Interfaces.INotificationTransform{TNotification}"/> type
    /// </summary>
    public Type TransformType { get; } = transformType;
}

/// <summary>
/// Recorded <c>Export</c> node.
/// </summary>
internal sealed class GraphExportDefinition<TNotification, TEvent>(Type exporterType)
        : GraphNodeDefinition<TNotification, TEvent>
{
    /// <summary>
    /// Registered <see cref="Exporters.Interfaces.INotificationExporter{TNotification}"/> type
    /// </summary>
    public Type ExporterType { get; } = exporterType;
}

/// <summary>
/// Recorded <c>Custom</c> node: either a custom service type or an inline lambda.
/// </summary>
internal sealed class GraphCustomDefinition<TNotification, TEvent>(
    Type? customType,
    Func<TNotification, CancellationToken, ValueTask<TNotification>>? invocation)
        : GraphNodeDefinition<TNotification, TEvent>
{
    /// <summary>
    /// Registered <see cref="Custom.Interfaces.INotificationCustom{TNotification}"/> type, when class-based
    /// </summary>
    public Type? CustomType { get; } = customType;

    /// <summary>
    /// Inline invocation lambda, when lambda-based
    /// </summary>
    public Func<TNotification, CancellationToken, ValueTask<TNotification>>? Invocation { get; } = invocation;
}

/// <summary>
/// Registration-time <c>Branch</c> node holding the child builders of every branch sub-graph.
/// Frozen into a <see cref="GraphBranchDefinition{TNotification,TEvent}"/> with built sub-plans.
/// </summary>
internal sealed class GraphBranchRegistration<TNotification, TEvent>(
    BranchPolicy? policyOverride,
    SectorGraphBuilder<TNotification, TEvent>[] branchBuilders)
        : GraphNodeDefinition<TNotification, TEvent>
{
    /// <summary>
    /// Per-node policy override; <see langword="null"/> falls back to FailFast
    /// </summary>
    public BranchPolicy? PolicyOverride { get; } = policyOverride;

    /// <summary>
    /// Builders of every branch sub-graph, in registration order
    /// </summary>
    public SectorGraphBuilder<TNotification, TEvent>[] BranchBuilders { get; } = branchBuilders;
}

/// <summary>
/// Frozen <c>Branch</c> node of the executable plan.
/// </summary>
internal sealed class GraphBranchDefinition<TNotification, TEvent>(
    BranchPolicy? policyOverride,
    SectorGraphPlan<TNotification, TEvent>[] branchPlans)
        : GraphNodeDefinition<TNotification, TEvent>
{
    /// <summary>
    /// Per-node policy override; <see langword="null"/> falls back to FailFast
    /// </summary>
    public BranchPolicy? PolicyOverride { get; } = policyOverride;

    /// <summary>
    /// Frozen sub-plans of every branch, in registration order
    /// </summary>
    public SectorGraphPlan<TNotification, TEvent>[] BranchPlans { get; } = branchPlans;
}

/// <summary>
/// Recorded and frozen <c>Join</c> node.
/// </summary>
internal sealed class GraphJoinDefinition<TNotification, TEvent>(Type joinType)
        : GraphNodeDefinition<TNotification, TEvent>
{
    /// <summary>
    /// Registered <see cref="Join.Interfaces.INotificationJoin{TNotification}"/> reducer type
    /// </summary>
    public Type JoinType { get; } = joinType;
}

/// <summary>
/// Immutable executable plan of one sector graph: conditions, the map source and
/// the ordered main-path nodes. Branch sub-plans reuse this shape with empty
/// conditions and no map (branches start from the mapped notification).
/// </summary>
/// <param name="conditionTypes">
///     Condition types evaluated against the raw event, in registration order; empty for branch sub-plans
/// </param>
/// <param name="map">
///     The single map source of the sector; always set for the top-level plan
///     (guaranteed by validation), <see langword="null"/> for branch sub-plans
/// </param>
/// <param name="nodes">Ordered main-path nodes executed after the map; for branch sub-plans this is the whole branch body</param>
/// <param name="defaultBranchPolicy">
///     Sector-level default policy for fan-outs without their own override;
///     <see langword="null"/> falls back to <see cref="BranchPolicy.FailFast"/> at execution
/// </param>
/// <param name="execution">Execution mode requested for this sector (see <see cref="SectorExecution"/>)</param>
internal sealed class SectorGraphPlan<TNotification, TEvent>(
    Type[] conditionTypes,
    GraphMapRegistration<TNotification, TEvent>? map,
    GraphNodeDefinition<TNotification, TEvent>[] nodes,
    BranchPolicy? defaultBranchPolicy = null,
    SectorExecution execution = SectorExecution.Auto)
{
    /// <summary>
    /// Condition types evaluated against the raw event, in registration order.
    /// Empty for branch sub-plans.
    /// </summary>
    public Type[] ConditionTypes { get; } = conditionTypes;

    /// <summary>
    /// The single map source of the sector. Always set for the top-level plan
    /// (guaranteed by validation); <see langword="null"/> for branch sub-plans.
    /// </summary>
    public GraphMapRegistration<TNotification, TEvent>? Map { get; } = map;

    /// <summary>
    /// Ordered main-path nodes executed after the map. For branch sub-plans this
    /// is the whole branch body.
    /// </summary>
    public GraphNodeDefinition<TNotification, TEvent>[] Nodes { get; } = nodes;

    /// <summary>
    /// Sector-level default policy applied to fan-outs that do not carry their
    /// own <see cref="GraphBranchDefinition{TNotification,TEvent}.PolicyOverride"/>.
    /// <see langword="null"/> falls back to <see cref="BranchPolicy.FailFast"/>.
    /// </summary>
    public BranchPolicy? DefaultBranchPolicy { get; } = defaultBranchPolicy;

    /// <summary>
    /// Execution mode requested for this sector. The effective path (compiled vs
    /// scoped) is selected at executor startup by <see cref="SectorGraphCompiler"/>.
    /// </summary>
    public SectorExecution Execution { get; } = execution;
}
