using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Config;
using Notifliwy.Custom.Interfaces;
using Notifliwy.Exceptions;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Graph.Interfaces;
using Notifliwy.Graph.Internals;
using Notifliwy.Join.Interfaces;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Transform.Interfaces;

namespace Notifliwy.Graph;

/// <summary>
/// Concrete <see cref="ISectorGraphBuilder{TNotification,TEvent}"/> accumulating graph
/// registrations in call order. <see cref="BuildPlan()"/> validates the structure and
/// freezes it into an immutable executable plan.
/// </summary>
/// <typeparam name="TNotification">The notification type produced by the <c>Map</c> node</typeparam>
/// <typeparam name="TEvent">The event type consumed by the sector</typeparam>
public class SectorGraphBuilder<TNotification, TEvent> : ISectorGraphBuilder<TNotification, TEvent>
{
    /// <summary>
    /// Create a top-level sector graph builder.
    /// </summary>
    public SectorGraphBuilder()
            : this(branchScope: false)
    {
    }

    internal SectorGraphBuilder(bool branchScope)
    {
        BranchScope = branchScope;
    }

    /// <summary>
    /// Ordered registration entries in fluent call order.
    /// </summary>
    internal List<GraphRegistration> Registrations { get; } = [];

    /// <summary>
    /// <see langword="true"/> for builders handed to <c>Branch</c> actions,
    /// where <c>When</c> and <c>Map</c> are not allowed.
    /// </summary>
    internal bool BranchScope { get; }

    /// <inheritdoc />
    public ISectorGraphBuilder<TNotification, TEvent> When<TCondition>()
            where TCondition : class, INotificationCondition<TNotification, TEvent>
    {
        Registrations.Add(new GraphWhenRegistration(typeof(TCondition)));
        return this;
    }

    /// <inheritdoc />
    public ISectorGraphBuilder<TNotification, TEvent> Map<TMapper>()
            where TMapper : class, INotificationMapper<TNotification, TEvent>
    {
        Registrations.Add(new GraphMapRegistration<TNotification, TEvent>(typeof(TMapper), mapping: null));
        return this;
    }

    /// <inheritdoc />
    public ISectorGraphBuilder<TNotification, TEvent> Map(
        Func<TEvent, CancellationToken, ValueTask<TNotification>> mapping)
    {
        Registrations.Add(new GraphMapRegistration<TNotification, TEvent>(mapperType: null, mapping));
        return this;
    }

    /// <inheritdoc />
    public ISectorGraphBuilder<TNotification, TEvent> Transform<TTransform>()
            where TTransform : class, INotificationTransform<TNotification>
    {
        Registrations.Add(new GraphTransformDefinition<TNotification, TEvent>(typeof(TTransform)));
        return this;
    }

    /// <inheritdoc />
    public ISectorGraphBuilder<TNotification, TEvent> Export<TExporter>()
            where TExporter : class, INotificationExporter<TNotification>
    {
        Registrations.Add(new GraphExportDefinition<TNotification, TEvent>(typeof(TExporter)));
        return this;
    }

    /// <inheritdoc />
    public ISectorGraphBuilder<TNotification, TEvent> Branch(
        params Action<ISectorGraphBuilder<TNotification, TEvent>>[] branches)
    {
        return Branch(policyOverride: null, branches);
    }

    /// <inheritdoc />
    public ISectorGraphBuilder<TNotification, TEvent> Branch(
        BranchPolicy policy,
        params Action<ISectorGraphBuilder<TNotification, TEvent>>[] branches)
    {
        return Branch(policyOverride: policy, branches);
    }

    private ISectorGraphBuilder<TNotification, TEvent> Branch(
        BranchPolicy? policyOverride,
        Action<ISectorGraphBuilder<TNotification, TEvent>>[] branches)
    {
        var branchBuilders = branches
                .Select(branchAction =>
                {
                    var branchBuilder = new SectorGraphBuilder<TNotification, TEvent>(branchScope: true);
                    branchAction.Invoke(branchBuilder);
                    return branchBuilder;
                })
                .ToArray();

        Registrations.Add(new GraphBranchRegistration<TNotification, TEvent>(policyOverride, branchBuilders));
        return this;
    }

    /// <inheritdoc />
    public ISectorGraphBuilder<TNotification, TEvent> Join<TJoin>()
            where TJoin : class, INotificationJoin<TNotification>
    {
        Registrations.Add(new GraphJoinDefinition<TNotification, TEvent>(typeof(TJoin)));
        return this;
    }

    /// <inheritdoc />
    public ISectorGraphBuilder<TNotification, TEvent> Custom<TCustom>()
            where TCustom : class, INotificationCustom<TNotification>
    {
        Registrations.Add(new GraphCustomDefinition<TNotification, TEvent>(typeof(TCustom), invocation: null));
        return this;
    }

    /// <inheritdoc />
    public ISectorGraphBuilder<TNotification, TEvent> Custom(
        Func<TNotification, CancellationToken, ValueTask<TNotification>> invocation)
    {
        Registrations.Add(new GraphCustomDefinition<TNotification, TEvent>(customType: null, invocation));
        return this;
    }

    /// <summary>
    /// Validate the recorded structure and freeze it into an immutable
    /// <see cref="SectorGraphPlan{TNotification,TEvent}"/>. Throws
    /// <see cref="SectorGraphValidationException"/> naming this sector on any violation.
    /// </summary>
    internal SectorGraphPlan<TNotification, TEvent> BuildPlan()
    {
        return BuildPlan(sectorBranchPolicy: null, SectorExecution.Auto);
    }

    /// <summary>
    /// Validate the recorded structure and freeze it into an immutable
    /// <see cref="SectorGraphPlan{TNotification,TEvent}"/> carrying sector-level
    /// options. Throws <see cref="SectorGraphValidationException"/> naming this
    /// sector on any violation.
    /// </summary>
    /// <param name="sectorBranchPolicy">
    ///     sector-level default policy for fan-outs without their own override;
    ///     <see langword="null"/> falls back to <see cref="BranchPolicy.FailFast"/>
    /// </param>
    /// <param name="execution">execution mode requested for this sector</param>
    internal SectorGraphPlan<TNotification, TEvent> BuildPlan(
        BranchPolicy? sectorBranchPolicy,
        SectorExecution execution)
    {
        var violations = new List<string>();
        SectorGraphValidator.CollectViolations<TNotification, TEvent>(Registrations, BranchScope, violations);

        if (violations.Count > 0)
        {
            throw new SectorGraphValidationException(SectorName(), violations);
        }

        return Freeze(sectorBranchPolicy, execution);
    }

    /// <summary>
    /// Register the graph into <paramref name="serviceCollection"/>: node service types
    /// as scoped services (skipping types the host already registered, so custom
    /// lifetimes and instances win), the frozen plan and the executor as singletons.
    /// Validation runs here, so a broken graph fails at startup registration.
    /// </summary>
    internal void RegisterGraph(IServiceCollection serviceCollection)
    {
        var plan = BuildPlan();

        foreach (var conditionType in plan.ConditionTypes)
        {
            RegisterIfMissing(serviceCollection, conditionType);
        }

        if (plan.Map?.MapperType is { } mapperType)
        {
            RegisterIfMissing(serviceCollection, mapperType);
        }

        foreach (var serviceType in CollectNodeServiceTypes(plan))
        {
            RegisterIfMissing(serviceCollection, serviceType);
        }

        serviceCollection.AddSingleton(plan);
        serviceCollection.AddSectorGraphExecutor<TNotification, TEvent>();
    }

    private static void RegisterIfMissing(
        IServiceCollection serviceCollection,
        Type serviceType)
    {
        if (serviceCollection.All(descriptor => descriptor.ServiceType != serviceType))
        {
            serviceCollection.AddScoped(serviceType);
        }
    }

    private static IEnumerable<Type> CollectNodeServiceTypes(SectorGraphPlan<TNotification, TEvent> plan)
    {
        foreach (var node in plan.Nodes)
        {
            switch (node)
            {
                case GraphTransformDefinition<TNotification, TEvent> transformDefinition:
                    yield return transformDefinition.TransformType;
                    break;

                case GraphExportDefinition<TNotification, TEvent> exportDefinition:
                    yield return exportDefinition.ExporterType;
                    break;

                case GraphCustomDefinition<TNotification, TEvent> customDefinition:
                    if (customDefinition.CustomType is { } customType)
                    {
                        yield return customType;
                    }

                    break;

                case GraphJoinDefinition<TNotification, TEvent> joinDefinition:
                    yield return joinDefinition.JoinType;
                    break;

                case GraphBranchDefinition<TNotification, TEvent> branchDefinition:
                    foreach (var branchPlan in branchDefinition.BranchPlans)
                    {
                        foreach (var serviceType in CollectNodeServiceTypes(branchPlan))
                        {
                            yield return serviceType;
                        }
                    }

                    break;
            }
        }
    }

    private SectorGraphPlan<TNotification, TEvent> Freeze(
        BranchPolicy? sectorBranchPolicy,
        SectorExecution execution)
    {
        var conditionTypes = Registrations
                .OfType<GraphWhenRegistration>()
                .Select(registration => registration.ConditionType)
                .ToArray();

        var map = Registrations
            .OfType<GraphMapRegistration<TNotification, TEvent>>()
            .FirstOrDefault();

        var nodes = Registrations
                .OfType<GraphNodeDefinition<TNotification, TEvent>>()
                .Select(FreezeNode)
                .ToArray();

        return new SectorGraphPlan<TNotification, TEvent>(conditionTypes, map, nodes, sectorBranchPolicy, execution);
    }

    private GraphNodeDefinition<TNotification, TEvent> FreezeNode(
        GraphNodeDefinition<TNotification, TEvent> node)
    {
        if (node is not GraphBranchRegistration<TNotification, TEvent> branch)
        {
            return node;
        }

        var branchPlans = branch.BranchBuilders
                .Select(branchBuilder => branchBuilder.BuildPlan())
                .ToArray();

        return new GraphBranchDefinition<TNotification, TEvent>(branch.PolicyOverride, branchPlans);
    }

    private string SectorName()
    {
        return $"{typeof(TNotification).Name}/{typeof(TEvent).Name}";
    }
}
