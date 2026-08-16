using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Custom.Interfaces;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Graph.Internals;
using Notifliwy.Join.Interfaces;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Transform.Interfaces;

namespace Notifliwy.Graph.Internals;

/// <summary>
/// Executes one sector graph plan for a single event: conditions → map → node walk.
/// Branch fan-outs run their sub-graphs in parallel (<see cref="Task.WhenAll"/>)
/// under the node's <see cref="BranchPolicy"/>; a join reduces the branch outputs
/// back into the main path (single-branch join is a passthrough that skips the reducer).
/// </summary>
/// <typeparam name="TNotification">The notification type produced by the <c>Map</c> node</typeparam>
/// <typeparam name="TEvent">The event type consumed by the sector</typeparam>
internal sealed class SectorGraphExecutor<TNotification, TEvent>(
    SectorGraphPlan<TNotification, TEvent> plan,
    IServiceScopeFactory scopeFactory,
    ILogger<SectorGraphExecutor<TNotification, TEvent>>? logger = null)
{
    /// <summary>
    /// The main method that processes a single event through the whole graph,
    /// resolving node services from a fresh DI scope.
    /// </summary>
    public async ValueTask ExecuteAsync(
        TEvent inputEvent,
        CancellationToken cancellationToken = default)
    {
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
            var condition = (INotificationCondition<TNotification, TEvent>)serviceProvider
                    .GetRequiredService(conditionType);

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
            var mapper = (INotificationMapper<TNotification, TEvent>)serviceProvider
                    .GetRequiredService(mapperType);

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
                    var transform = (INotificationTransform<TNotification>)serviceProvider
                            .GetRequiredService(transformDefinition.TransformType);

                    current = await transform.TransformAsync(current, cancellationToken);
                    break;
                }

                case GraphCustomDefinition<TNotification, TEvent> customDefinition:
                {
                    if (customDefinition.CustomType is { } customType)
                    {
                        var custom = (INotificationCustom<TNotification>)serviceProvider
                                .GetRequiredService(customType);

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
                    var exporter = (INotificationExporter<TNotification>)serviceProvider
                            .GetRequiredService(exportDefinition.ExporterType);

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
                        var join = (INotificationJoin<TNotification>)serviceProvider
                                .GetRequiredService(joinDefinition.JoinType);

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
        var policy = branchDefinition.PolicyOverride ?? BranchPolicy.FailFast;

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
}
