using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Custom.Interfaces;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Join.Interfaces;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Transform.Interfaces;

namespace Notifliwy.Graph.Internals;

/// <summary>
/// Runtime plan compiler: at startup it inspects node registrations against the DI
/// descriptor list, decides whether a sector graph can take the compiled hot path
/// (H1b), and — when it can — resolves every node instance once and freezes the
/// plan into a <see cref="CompiledSectorPlan{TNotification,TEvent}"/> of direct
/// invokes. This is deliberately a runtime compiler rather than a source generator:
/// sector graphs are described through <see cref="ISectorGraphBuilder{TNotification,TEvent}"/>
/// calls that run at plan-materialization time, so the graph shape itself is only
/// knowable at runtime; the source generator (Generator B) covers registration
/// discovery only.
/// </summary>
internal static class SectorGraphCompiler
{
    /// <summary>
    /// Try to compile <paramref name="plan"/> onto the compiled hot path. Returns
    /// the compiled instance graph when every node is compile-safe, otherwise
    /// <see langword="null"/> with a blocker reason per offending node (deduplicated,
    /// in walk order) in <paramref name="blockers"/>.
    /// </summary>
    /// <param name="plan">frozen sector graph plan</param>
    /// <param name="rootProvider">root service provider used to resolve node instances once</param>
    /// <param name="descriptors">DI registration descriptors analyzed for compile-safety</param>
    /// <param name="blockers">collector for per-node blockers, empty when compilable</param>
    public static CompiledSectorPlan<TNotification, TEvent>? TryCompile<TNotification, TEvent>(
        SectorGraphPlan<TNotification, TEvent> plan,
        IServiceProvider rootProvider,
        IEnumerable<ServiceDescriptor> descriptors,
        out IReadOnlyList<string> blockers)
    {
        var blockerList = new List<string>();

        foreach (var nodeType in CollectNodeTypes<TNotification, TEvent>(plan))
        {
            var reason = TryGetUnsafeReason(nodeType, descriptors);

            if (reason is not null)
            {
                blockerList.Add($"{nodeType.Name} {reason}");
            }
        }

        blockers = blockerList;

        return blockerList.Count == 0
            ? Compile<TNotification, TEvent>(plan, rootProvider, descriptors)
            : null;
    }

    /// <summary>
    /// Collect every service type referenced by the plan: conditions, the mapper,
    /// and all post-map nodes including branch sub-plans.
    /// </summary>
    private static IEnumerable<Type> CollectNodeTypes<TNotification, TEvent>(
        SectorGraphPlan<TNotification, TEvent> plan)
    {
        foreach (var conditionType in plan.ConditionTypes)
        {
            yield return conditionType;
        }

        if (plan.Map?.MapperType is { } mapperType)
        {
            yield return mapperType;
        }

        foreach (var nodeType in CollectPlanNodeTypes<TNotification, TEvent>(plan))
        {
            yield return nodeType;
        }
    }

    private static IEnumerable<Type> CollectPlanNodeTypes<TNotification, TEvent>(
        SectorGraphPlan<TNotification, TEvent> plan)
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
                        foreach (var branchNodeType in CollectPlanNodeTypes<TNotification, TEvent>(branchPlan))
                        {
                            yield return branchNodeType;
                        }
                    }

                    break;
            }
        }
    }

    /// <summary>
    /// Build the compiled instance graph. Every node type has already been proven
    /// compile-safe by <paramref name="descriptors"/> analysis, so resolution here
    /// cannot capture a scoped dependency.
    /// </summary>
    private static CompiledSectorPlan<TNotification, TEvent> Compile<TNotification, TEvent>(
        SectorGraphPlan<TNotification, TEvent> plan,
        IServiceProvider rootProvider,
        IEnumerable<ServiceDescriptor> descriptors)
    {
        var conditions = plan.ConditionTypes
                .Select(conditionType => (INotificationCondition<TNotification, TEvent>)ResolveCompileSafe(
                    conditionType,
                    rootProvider,
                    descriptors))
                .ToArray();

        Func<TEvent, CancellationToken, ValueTask<TNotification>>? map = plan.Map?.Mapping;

        if (plan.Map?.MapperType is { } mapperType)
        {
            var mapper = (INotificationMapper<TNotification, TEvent>)ResolveCompileSafe(
                mapperType,
                rootProvider,
                descriptors);

            map = CompiledNodeWrappers.WrapMapper<TNotification, TEvent>(mapper);
        }

        return new CompiledSectorPlan<TNotification, TEvent>(
            conditions,
            map,
            CompileNodes<TNotification, TEvent>(plan.Nodes, rootProvider, descriptors));
    }

    private static CompiledNodeDefinition<TNotification, TEvent>[] CompileNodes<TNotification, TEvent>(
        GraphNodeDefinition<TNotification, TEvent>[] nodes,
        IServiceProvider rootProvider,
        IEnumerable<ServiceDescriptor> descriptors)
    {
        var compiledNodes = new List<CompiledNodeDefinition<TNotification, TEvent>>(nodes.Length);

        foreach (var node in nodes)
        {
            switch (node)
            {
                case GraphTransformDefinition<TNotification, TEvent> transformDefinition:
                {
                    var transform = (INotificationTransform<TNotification>)ResolveCompileSafe(
                        transformDefinition.TransformType,
                        rootProvider,
                        descriptors);

                    compiledNodes.Add(new CompiledTransformNode<TNotification, TEvent>(transform));
                    break;
                }

                case GraphCustomDefinition<TNotification, TEvent> customDefinition:
                {
                    Func<TNotification, CancellationToken, ValueTask<TNotification>> invocation =
                        customDefinition.Invocation!;

                    if (customDefinition.CustomType is { } customType)
                    {
                        var custom = (INotificationCustom<TNotification>)ResolveCompileSafe(
                            customType,
                            rootProvider,
                            descriptors);

                        invocation = CompiledNodeWrappers.WrapCustom<TNotification, TEvent>(custom);
                    }

                    compiledNodes.Add(new CompiledCustomNode<TNotification, TEvent>(invocation));
                    break;
                }

                case GraphExportDefinition<TNotification, TEvent> exportDefinition:
                {
                    var exporter = (INotificationExporter<TNotification>)ResolveCompileSafe(
                        exportDefinition.ExporterType,
                        rootProvider,
                        descriptors);

                    compiledNodes.Add(new CompiledExportNode<TNotification, TEvent>(exporter));
                    break;
                }

                case GraphBranchDefinition<TNotification, TEvent> branchDefinition:
                {
                    var branchPlans = branchDefinition.BranchPlans
                            .Select(branchPlan => new CompiledSectorPlan<TNotification, TEvent>(
                                [],
                                map: null,
                                CompileNodes<TNotification, TEvent>(
                                    branchPlan.Nodes,
                                    rootProvider,
                                    descriptors)))
                            .ToArray();

                    compiledNodes.Add(
                        new CompiledBranchNode<TNotification, TEvent>(branchDefinition.PolicyOverride, branchPlans));
                    break;
                }

                case GraphJoinDefinition<TNotification, TEvent> joinDefinition:
                {
                    var join = (INotificationJoin<TNotification>)ResolveCompileSafe(
                        joinDefinition.JoinType,
                        rootProvider,
                        descriptors);

                    compiledNodes.Add(new CompiledJoinNode<TNotification, TEvent>(join));
                    break;
                }
            }
        }

        return compiledNodes.ToArray();
    }

    /// <summary>
    /// Resolve a compile-safe node instance exactly once: through its DI
    /// registration (singleton or singleton-dependency transient) when present,
    /// otherwise by its verified public parameterless constructor.
    /// </summary>
    private static object ResolveCompileSafe(
        Type nodeType,
        IServiceProvider rootProvider,
        IEnumerable<ServiceDescriptor> descriptors)
    {
        var descriptor = FindDescriptor(nodeType, descriptors);

        return descriptor is null
            ? Activator.CreateInstance(nodeType)!
            : rootProvider.GetRequiredService(descriptor.ServiceType);
    }

    /// <summary>
    /// Decide whether a node type is compile-safe and, when it is not, produce the
    /// reason. Safe means: registered singleton; or registered transient with only
    /// singleton-safe constructor dependencies; or not registered with a public
    /// parameterless constructor (stateless shape). Unregistered nodes with
    /// constructor dependencies cannot be proven safe and are rejected.
    /// </summary>
    private static string? TryGetUnsafeReason(
        Type nodeType,
        IEnumerable<ServiceDescriptor> descriptors)
    {
        var descriptor = FindDescriptor(nodeType, descriptors);

        if (descriptor is null)
        {
            return HasPublicParameterlessConstructor(nodeType)
                ? null
                : "is not registered in DI and has no public parameterless constructor, "
                        + "so its constructor dependencies cannot be proven singleton-safe";
        }

        switch (descriptor.Lifetime)
        {
            case ServiceLifetime.Singleton:
                return null;

            case ServiceLifetime.Scoped:
                return "is registered with Scoped lifetime — a compiled sector would hold it captive";

            case ServiceLifetime.Transient:
                return IsTransientSingletonSafe(nodeType, descriptors)
                    ? null
                    : "is registered Transient with constructor dependencies that are not singleton-safe";

            default:
                return "is registered with an unknown lifetime";
        }
    }

    /// <summary>
    /// A transient registration is singleton-safe when it has a public parameterless
    /// constructor, or some public constructor whose every parameter is itself
    /// singleton-registered or an unregistered stateless (parameterless) type.
    /// </summary>
    private static bool IsTransientSingletonSafe(
        Type nodeType,
        IEnumerable<ServiceDescriptor> descriptors)
    {
        if (HasPublicParameterlessConstructor(nodeType))
        {
            return true;
        }

        return nodeType.GetConstructors()
            .Any(constructor => constructor.GetParameters()
                .All(parameter => IsSingletonSafeDependency(parameter.ParameterType, descriptors)));
    }

    private static bool IsSingletonSafeDependency(
        Type dependencyType,
        IEnumerable<ServiceDescriptor> descriptors)
    {
        var descriptor = FindDescriptor(dependencyType, descriptors);

        return descriptor is null
            ? HasPublicParameterlessConstructor(dependencyType)
            : descriptor.Lifetime == ServiceLifetime.Singleton;
    }

    private static bool HasPublicParameterlessConstructor(Type nodeType)
    {
        return nodeType.GetConstructor(Type.EmptyTypes) is not null;
    }

    /// <summary>
    /// Find the effective DI descriptor for a node type: the last registration
    /// exposing it as the service type (matching DI last-wins semantics), falling
    /// back to the last registration using it as the implementation behind an
    /// interface service type.
    /// </summary>
    private static ServiceDescriptor? FindDescriptor(
        Type nodeType,
        IEnumerable<ServiceDescriptor> descriptors)
    {
        ServiceDescriptor? byServiceType = null;
        ServiceDescriptor? byImplementationType = null;

        foreach (var descriptor in descriptors)
        {
            if (descriptor.ServiceType == nodeType)
            {
                byServiceType = descriptor;
            }
            else if (descriptor.ImplementationType == nodeType)
            {
                byImplementationType = descriptor;
            }
        }

        return byServiceType ?? byImplementationType;
    }
}
