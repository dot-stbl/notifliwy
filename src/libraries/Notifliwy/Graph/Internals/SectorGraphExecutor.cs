using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Config.Internals;
using Notifliwy.Custom.Interfaces;
using Notifliwy.Exceptions;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Join.Interfaces;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Transform.Interfaces;

namespace Notifliwy.Graph.Internals;

/// <summary>
/// Executes one sector graph plan for a single event. At construction (startup for
/// a hosted server) the executor asks <see cref="SectorGraphCompiler"/> to select
/// the execution path per the sector's <see cref="Config.SectorExecution"/>:
/// <list type="bullet">
///     <item><b>Compiled</b> — every node resolved or constructed once, direct invokes,
///     no per-event DI scope. <c>Compiled</c> mode fails fast with
///     <see cref="SectorCaptiveDependencyException"/> on scoped/unprovable nodes;
///     <c>Auto</c> falls back to the scoped path with a logged reason.</item>
///     <item><b>Scoped</b> — conditions → map → node walk, every node resolved from a
///     fresh DI scope. Branch fan-outs run their sub-graphs in parallel
///     (<see cref="Task.WhenAll"/>) under the node's <see cref="BranchPolicy"/>; a
///     join reduces the branch outputs back into the main path (single-branch join
///     is a passthrough that skips the reducer).</item>
/// </list>
/// </summary>
/// <typeparam name="TNotification">The notification type produced by the <c>Map</c> node</typeparam>
/// <typeparam name="TEvent">The event type consumed by the sector</typeparam>
internal sealed class SectorGraphExecutor<TNotification, TEvent>(
    SectorGraphPlan<TNotification, TEvent> plan,
    IServiceScopeFactory scopeFactory,
    IServiceProvider rootProvider,
    IServiceCollection serviceCollection,
    ILogger<SectorGraphExecutor<TNotification, TEvent>>? logger = null)
{
    private readonly ExecutionSelection selection = SelectExecution(plan, rootProvider, serviceCollection, logger);

    /// <summary>
    /// Effective execution decision made at startup: the chosen mode and, for the
    /// scoped path, the reasons that blocked compilation. Exposed for tests and
    /// diagnostics.
    /// </summary>
    public SectorExecutionDecision Decision => selection.Decision;

    /// <summary>
    /// The main method that processes a single event through the whole graph on the
    /// selected path: compiled instance graph, or a fresh DI scope per event.
    /// </summary>
    public async ValueTask ExecuteAsync(
        TEvent inputEvent,
        CancellationToken cancellationToken = default)
    {
        if (selection.Compiled is { } compiled)
        {
            await ExecuteCompiledAsync(compiled, inputEvent, cancellationToken);
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();

        await ExecuteScopeAsync(scope.ServiceProvider, inputEvent, cancellationToken);
    }

    /// <summary>
    /// Process a single event against an already created scope — entry point for
    /// hosting that manages per-event scopes itself.
    /// </summary>
    internal async ValueTask ExecuteScopeAsync(
        IServiceProvider serviceProvider,
        TEvent inputEvent,
        CancellationToken cancellationToken = default)
    {
        foreach (var conditionType in plan.ConditionTypes)
        {
            var condition = (INotificationCondition<TNotification, TEvent>)ResolveNode(
                serviceProvider,
                conditionType);

            if (!await condition.AllowItAsync(inputEvent, cancellationToken))
            {
                return;
            }
        }

        if (plan.Map is not { } map)
        {
            // top-level plans always carry a Map (guaranteed by validation); branch
            // sub-plans are walked through RunNodesAsync directly and never reach here
            return;
        }

        TNotification current;

        if (map.MapperType is { } mapperType)
        {
            var mapper = (INotificationMapper<TNotification, TEvent>)ResolveNode(
                serviceProvider,
                mapperType);

            current = await mapper.ConvertAsync(inputEvent, cancellationToken);
        }
        else if (map.Mapping is { } mapping)
        {
            current = await mapping(inputEvent, cancellationToken);
        }
        else
        {
            return;
        }

        await RunNodesAsync(serviceProvider, plan.Nodes, current, cancellationToken);
    }

    private static object ResolveNode(
        IServiceProvider serviceProvider,
        Type nodeType)
    {
        // graphs registered inline have their node types pre-registered as scoped
        // services; graphs materialized from config classes resolve lazily, so fall
        // back to constructing the node from the per-event scope (same scoped semantics)
        return serviceProvider.GetService(nodeType)
                ?? ActivatorUtilities.CreateInstance(serviceProvider, nodeType);
    }

    private async ValueTask<TNotification> RunNodesAsync(
        IServiceProvider serviceProvider,
        IReadOnlyList<GraphNodeDefinition<TNotification, TEvent>> nodes,
        TNotification current,
        CancellationToken cancellationToken)
    {
        TNotification[]? pendingBranchOutputs = null;

        foreach (var node in nodes)
        {
            switch (node)
            {
                case GraphTransformDefinition<TNotification, TEvent> transformDefinition:
                {
                    var transform = (INotificationTransform<TNotification>)ResolveNode(
                        serviceProvider,
                        transformDefinition.TransformType);

                    current = await transform.TransformAsync(current, cancellationToken);
                    break;
                }

                case GraphCustomDefinition<TNotification, TEvent> customDefinition:
                {
                    if (customDefinition.CustomType is { } customType)
                    {
                        var custom = (INotificationCustom<TNotification>)ResolveNode(
                            serviceProvider,
                            customType);

                        current = await custom.InvokeAsync(current, cancellationToken);
                    }
                    else if (customDefinition.Invocation is { } invocation)
                    {
                        current = await invocation(current, cancellationToken);
                    }

                    break;
                }

                case GraphExportDefinition<TNotification, TEvent> exportDefinition:
                {
                    var exporter = (INotificationExporter<TNotification>)ResolveNode(
                        serviceProvider,
                        exportDefinition.ExporterType);

                    await exporter.ThrowAsync(current, cancellationToken);
                    break;
                }

                case GraphBranchDefinition<TNotification, TEvent> branchDefinition:
                {
                    // the fan-out does not change the main-path notification until a Join consumes the outputs
                    pendingBranchOutputs = await RunBranchesAsync(
                        serviceProvider,
                        branchDefinition,
                        current,
                        cancellationToken);
                    break;
                }

                case GraphJoinDefinition<TNotification, TEvent> joinDefinition:
                {
                    if (pendingBranchOutputs is not { } branchOutputs)
                    {
                        // unreachable for validated plans: Join always follows a Branch
                        break;
                    }

                    if (branchOutputs.Length == 1)
                    {
                        // single-branch join is a passthrough — the reducer is not invoked
                        current = branchOutputs[0];
                    }
                    else
                    {
                        var join = (INotificationJoin<TNotification>)ResolveNode(
                            serviceProvider,
                            joinDefinition.JoinType);

                        current = await join.JoinAsync(branchOutputs, cancellationToken);
                    }

                    pendingBranchOutputs = null;
                    break;
                }
            }
        }

        return current;
    }

    private async ValueTask<TNotification[]> RunBranchesAsync(
        IServiceProvider serviceProvider,
        GraphBranchDefinition<TNotification, TEvent> branchDefinition,
        TNotification input,
        CancellationToken cancellationToken)
    {
        var policy = branchDefinition.PolicyOverride
                ?? plan.DefaultBranchPolicy
                ?? BranchPolicy.FailFast;

        if (policy == BranchPolicy.FailFast)
        {
            var tasks = branchDefinition.BranchPlans
                    .Select(branchPlan => RunNodesAsync(
                            serviceProvider,
                            branchPlan.Nodes,
                            input,
                            cancellationToken)
                        .AsTask())
                    .ToArray();

            // Task.WhenAll observes every branch, then rethrows the first fault
            return await Task.WhenAll(tasks);
        }

        var outcomes = branchDefinition.BranchPlans
                .Select(branchPlan => RunBranchSafeAsync(serviceProvider, branchPlan, input, cancellationToken))
                .ToArray();

        await Task.WhenAll(outcomes);

        return outcomes
            .Where(outcome => outcome.Result.Survived)
            .Select(outcome => outcome.Result.Notification!)
            .ToArray();
    }

    private async Task<BranchOutcome> RunBranchSafeAsync(
        IServiceProvider serviceProvider,
        SectorGraphPlan<TNotification, TEvent> branchPlan,
        TNotification input,
        CancellationToken cancellationToken)
    {
        try
        {
            var notification = await RunNodesAsync(
                serviceProvider,
                branchPlan.Nodes,
                input,
                cancellationToken);

            return new BranchOutcome(notification, survived: true);
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger?.LogError(
                exception,
                "BestEffort branch failed and was skipped for notification {NotificationType}",
                typeof(TNotification).Name);

            return new BranchOutcome(default, survived: false);
        }
    }

    private sealed class BranchOutcome(TNotification? notification, bool survived)
    {
        public TNotification? Notification { get; } = notification;

        public bool Survived { get; } = survived;
    }

    /// <summary>
    /// Compiled-path execution: conditions → map → node walk over pre-resolved
    /// instances, mirroring the scoped walk node for node.
    /// </summary>
    private async ValueTask ExecuteCompiledAsync(
        CompiledSectorPlan<TNotification, TEvent> compiled,
        TEvent inputEvent,
        CancellationToken cancellationToken)
    {
        foreach (var condition in compiled.Conditions)
        {
            if (!await condition.AllowItAsync(inputEvent, cancellationToken))
            {
                return;
            }
        }

        if (compiled.Map is not { } map)
        {
            // unreachable: only branch sub-plans have no map and those run through
            // RunCompiledNodesAsync directly
            return;
        }

        var current = await map(inputEvent, cancellationToken);

        await RunCompiledNodesAsync(compiled.Nodes, current, cancellationToken);
    }

    private async ValueTask<TNotification> RunCompiledNodesAsync(
        IReadOnlyList<CompiledNodeDefinition<TNotification, TEvent>> nodes,
        TNotification current,
        CancellationToken cancellationToken)
    {
        TNotification[]? pendingBranchOutputs = null;

        foreach (var node in nodes)
        {
            switch (node)
            {
                case CompiledTransformNode<TNotification, TEvent> transformNode:
                {
                    current = await transformNode.Transform.TransformAsync(current, cancellationToken);
                    break;
                }

                case CompiledCustomNode<TNotification, TEvent> customNode:
                {
                    current = await customNode.Invocation(current, cancellationToken);
                    break;
                }

                case CompiledExportNode<TNotification, TEvent> exportNode:
                {
                    await exportNode.Exporter.ThrowAsync(current, cancellationToken);
                    break;
                }

                case CompiledBranchNode<TNotification, TEvent> branchNode:
                {
                    // the fan-out does not change the main-path notification until a Join consumes the outputs
                    pendingBranchOutputs = await RunCompiledBranchesAsync(branchNode, current, cancellationToken);
                    break;
                }

                case CompiledJoinNode<TNotification, TEvent> joinNode:
                {
                    if (pendingBranchOutputs is not { } branchOutputs)
                    {
                        // unreachable for validated plans: Join always follows a Branch
                        break;
                    }

                    if (branchOutputs.Length == 1)
                    {
                        // single-branch join is a passthrough — the reducer is not invoked
                        current = branchOutputs[0];
                    }
                    else
                    {
                        current = await joinNode.Join.JoinAsync(branchOutputs, cancellationToken);
                    }

                    pendingBranchOutputs = null;
                    break;
                }
            }
        }

        return current;
    }

    private async ValueTask<TNotification[]> RunCompiledBranchesAsync(
        CompiledBranchNode<TNotification, TEvent> branchNode,
        TNotification input,
        CancellationToken cancellationToken)
    {
        var policy = branchNode.PolicyOverride
                ?? plan.DefaultBranchPolicy
                ?? BranchPolicy.FailFast;

        if (policy == BranchPolicy.FailFast)
        {
            var tasks = branchNode.BranchPlans
                    .Select(branchPlan => RunCompiledNodesAsync(
                            branchPlan.Nodes,
                            input,
                            cancellationToken)
                        .AsTask())
                    .ToArray();

            // Task.WhenAll observes every branch, then rethrows the first fault
            return await Task.WhenAll(tasks);
        }

        var outcomes = branchNode.BranchPlans
                .Select(branchPlan => RunCompiledBranchSafeAsync(branchPlan, input, cancellationToken))
                .ToArray();

        await Task.WhenAll(outcomes);

        return outcomes
            .Where(outcome => outcome.Result.Survived)
            .Select(outcome => outcome.Result.Notification!)
            .ToArray();
    }

    private async Task<BranchOutcome> RunCompiledBranchSafeAsync(
        CompiledSectorPlan<TNotification, TEvent> branchPlan,
        TNotification input,
        CancellationToken cancellationToken)
    {
        try
        {
            var notification = await RunCompiledNodesAsync(
                branchPlan.Nodes,
                input,
                cancellationToken);

            return new BranchOutcome(notification, survived: true);
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger?.LogError(
                exception,
                "BestEffort branch failed and was skipped for notification {NotificationType}",
                typeof(TNotification).Name);

            return new BranchOutcome(default, survived: false);
        }
    }

    /// <summary>
    /// Outcome of the startup path selection: the decision plus the compiled
    /// instance graph when the compiled path was chosen.
    /// </summary>
    private sealed record ExecutionSelection(
        SectorExecutionDecision Decision,
        CompiledSectorPlan<TNotification, TEvent>? Compiled);

    /// <summary>
    /// Apply the sector's requested execution mode: force scoped, force compiled
    /// (failing fast on captive/unprovable dependencies), or auto-select with a
    /// logged fallback reason. Also resolves the one-shot assembly-scan warning
    /// notice, if the sectors of this provider were discovered by reflection.
    /// </summary>
    private static ExecutionSelection SelectExecution(
        SectorGraphPlan<TNotification, TEvent> plan,
        IServiceProvider rootProvider,
        IServiceCollection serviceCollection,
        ILogger? logger)
    {
        // force materialization of the reflection-fallback warning when present
        rootProvider.GetService<SectorAssemblyScanNotice>();

        var sectorName = $"{typeof(TNotification).Name}/{typeof(TEvent).Name}";

        switch (plan.Execution)
        {
            case Config.SectorExecution.Scoped:
            {
                const string forcedReason = "forced by SectorExecution.Scoped";

                logger?.LogInformation("Sector {SectorName}: scoped path (reason: {Reason})", sectorName, forcedReason);

                return new ExecutionSelection(
                    SectorExecutionDecision.ForScoped(forcedReason),
                    Compiled: null);
            }

            case Config.SectorExecution.Compiled:
            {
                var compiled = SectorGraphCompiler.TryCompile(plan, rootProvider, serviceCollection, out var blockers);

                if (compiled is null)
                {
                    throw new SectorCaptiveDependencyException(sectorName, blockers);
                }

                logger?.LogInformation("Sector {SectorName}: compiled path", sectorName);

                return new ExecutionSelection(SectorExecutionDecision.ForCompiled(), compiled);
            }

            default:
            {
                var compiled = SectorGraphCompiler.TryCompile(plan, rootProvider, serviceCollection, out var blockers);

                if (compiled is { } compiledPlan)
                {
                    logger?.LogInformation("Sector {SectorName}: compiled path", sectorName);

                    return new ExecutionSelection(SectorExecutionDecision.ForCompiled(), compiledPlan);
                }

                var reason = string.Join("; ", blockers);

                logger?.LogInformation(
                    "Sector {SectorName}: scoped path (reason: {Reason})",
                    sectorName,
                    reason);

                return new ExecutionSelection(
                    SectorExecutionDecision.ForScoped(blockers.ToArray()),
                    Compiled: null);
            }
        }
    }
}
