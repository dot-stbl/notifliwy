using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Config;
using Notifliwy.Config.Interfaces;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Dependency;
using Notifliwy.Exceptions;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Graph.Interfaces;
using Notifliwy.Graph.Internals;
using Notifliwy.Join.Interfaces;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Transform.Interfaces;
using Shouldly;
using Xunit;

namespace Notifliwy.Units.Graph;

/// <summary>
/// Unit tests for the compiled execution path (H1b): dual-path parity between
/// <see cref="SectorExecution.Compiled"/> and <see cref="SectorExecution.Scoped"/>,
/// the compiled-mode captive-dependency fail-fast and the Auto mode decision.
/// </summary>
public class CompiledExecutionTests
{
    private sealed class PositiveCondition(Recorder recorder) : INotificationCondition<GraphNotification, GraphEvent>
    {
        public ValueTask<bool> AllowItAsync(GraphEvent inputEvent, CancellationToken cancellationToken = default)
        {
            recorder.Record($"condition:{inputEvent.Value}");
            return ValueTask.FromResult(inputEvent.Value > 0);
        }
    }

    private sealed class DoublingMapper(Recorder recorder) : INotificationMapper<GraphNotification, GraphEvent>
    {
        public ValueTask<GraphNotification> ConvertAsync(
            GraphEvent inputEvent,
            CancellationToken cancellationToken = default)
        {
            recorder.Record($"map:{inputEvent.Value}");
            return ValueTask.FromResult(new GraphNotification { Value = inputEvent.Value * 2 });
        }
    }

    private sealed class AddTenTransform(Recorder recorder) : INotificationTransform<GraphNotification>
    {
        public ValueTask<GraphNotification> TransformAsync(
            GraphNotification notification,
            CancellationToken cancellationToken = default)
        {
            recorder.Record($"transform:{notification.Value}");
            return ValueTask.FromResult(new GraphNotification { Value = notification.Value + 10 });
        }
    }

    private sealed class DoublingBranchTransform(Recorder recorder) : INotificationTransform<GraphNotification>
    {
        public ValueTask<GraphNotification> TransformAsync(
            GraphNotification notification,
            CancellationToken cancellationToken = default)
        {
            recorder.Record($"branch-double:{notification.Value}");
            return ValueTask.FromResult(new GraphNotification { Value = notification.Value * 2 });
        }
    }

    private sealed class StampBranchExporter(Recorder recorder) : INotificationExporter<GraphNotification>
    {
        public ValueTask ThrowAsync(GraphNotification notification, CancellationToken cancellationToken = default)
        {
            recorder.Record($"branch-stamp:{notification.Value}");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class SumJoin(Recorder recorder) : INotificationJoin<GraphNotification>
    {
        public ValueTask<GraphNotification> JoinAsync(
            IReadOnlyList<GraphNotification> notifications,
            CancellationToken cancellationToken = default)
        {
            recorder.Record($"join:[{string.Join(",", notifications.Select(notification => notification.Value))}]");
            return ValueTask.FromResult(
                new GraphNotification { Value = notifications.Sum(notification => notification.Value) });
        }
    }

    private sealed class FinalExporter(Recorder recorder) : INotificationExporter<GraphNotification>
    {
        public ValueTask ThrowAsync(GraphNotification notification, CancellationToken cancellationToken = default)
        {
            recorder.Record($"final:{notification.Value}");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CompiledParityConfig : INotificationSectorConfig<GraphNotification, GraphEvent>
    {
        public SectorExecution Execution => SectorExecution.Compiled;

        public void Configure(ISectorGraphBuilder<GraphNotification, GraphEvent> graph)
        {
            ConfigureParityGraph(graph);
        }
    }

    private sealed class ScopedParityConfig : INotificationSectorConfig<GraphNotification, GraphEvent>
    {
        public SectorExecution Execution => SectorExecution.Scoped;

        public void Configure(ISectorGraphBuilder<GraphNotification, GraphEvent> graph)
        {
            ConfigureParityGraph(graph);
        }
    }

    private sealed class AutoParityConfig : INotificationSectorConfig<GraphNotification, GraphEvent>
    {
        public void Configure(ISectorGraphBuilder<GraphNotification, GraphEvent> graph)
        {
            ConfigureParityGraph(graph);
        }
    }

    private static void ConfigureParityGraph(ISectorGraphBuilder<GraphNotification, GraphEvent> graph)
    {
        graph
            .When<PositiveCondition>()
            .Map<DoublingMapper>()
            .Transform<AddTenTransform>()
            .Branch(
                branch => branch.Transform<DoublingBranchTransform>().Export<StampBranchExporter>(),
                branch => branch.Export<StampBranchExporter>())
            .Join<SumJoin>()
            .Export<FinalExporter>();
    }

    private static readonly Type[] ParityNodeTypes =
    [
        typeof(PositiveCondition),
        typeof(DoublingMapper),
        typeof(AddTenTransform),
        typeof(DoublingBranchTransform),
        typeof(StampBranchExporter),
        typeof(SumJoin),
        typeof(FinalExporter),
    ];

    private static ServiceCollection BuildParityServices<TConfig>(Recorder recorder, ServiceLifetime nodeLifetime)
            where TConfig : class, INotificationSectorConfig<GraphNotification, GraphEvent>
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(recorder);

        foreach (var nodeType in ParityNodeTypes)
        {
            switch (nodeLifetime)
            {
                case ServiceLifetime.Scoped:
                    services.AddScoped(nodeType);
                    break;

                default:
                    services.AddSingleton(nodeType);
                    break;
            }
        }

        services.AddNotifliwyServer(serverBuilder => serverBuilder.AddSector<TConfig>());

        return services;
    }

    [Fact]
    public async Task CompiledAndScopedPaths_ProduceIdenticalObservableBehaviour()
    {
        // Arrange - the same graph twice: node instances resolved once (compiled)
        // vs per-event scoped resolution; recorders capture every observable step
        var compiledRecorder = new Recorder();
        var scopedRecorder = new Recorder();

        await using var compiledProvider = BuildParityServices<CompiledParityConfig>(
                compiledRecorder,
                ServiceLifetime.Singleton)
            .BuildServiceProvider();

        await using var scopedProvider = BuildParityServices<ScopedParityConfig>(
                scopedRecorder,
                ServiceLifetime.Scoped)
            .BuildServiceProvider();

        var compiledExecutor = compiledProvider
            .GetRequiredService<SectorGraphExecutor<GraphNotification, GraphEvent>>();

        var scopedExecutor = scopedProvider
            .GetRequiredService<SectorGraphExecutor<GraphNotification, GraphEvent>>();

        compiledExecutor.Decision.Mode.ShouldBe(SectorExecutionMode.Compiled);
        scopedExecutor.Decision.Mode.ShouldBe(SectorExecutionMode.Scoped);

        var inputEvent = new GraphEvent { Value = 10 };

        // Act
        await compiledExecutor.ExecuteAsync(inputEvent);
        await scopedExecutor.ExecuteAsync(inputEvent);

        // Assert - branch bodies run in parallel, so compare as sorted multisets;
        // the join input order is deterministic (Task.WhenAll preserves branch order)
        compiledRecorder.Calls.OrderBy(call => call, StringComparer.Ordinal)
            .ShouldBe(scopedRecorder.Calls.OrderBy(call => call, StringComparer.Ordinal));

        compiledRecorder.Calls.ShouldContain("join:[60,30]");
        compiledRecorder.Calls.ShouldContain("final:90");
        scopedRecorder.Calls.ShouldContain("join:[60,30]");
        scopedRecorder.Calls.ShouldContain("final:90");
    }

    [Fact]
    public void CompiledMode_WithScopedRegisteredNode_FailsFastAtSectorResolution()
    {
        // Arrange - the full node set is singleton-registered except one node,
        // whose later Scoped registration wins (last-wins DI semantics)
        var services = BuildParityServices<CompiledParityConfig>(new Recorder(), ServiceLifetime.Singleton);
        services.AddScoped(typeof(AddTenTransform));

        using var serviceProvider = services.BuildServiceProvider();

        // Act + Assert - the exception names the sector and the offending node
        var exception = Should.Throw<SectorCaptiveDependencyException>(
            () => serviceProvider.GetRequiredService<SectorGraphExecutor<GraphNotification, GraphEvent>>());

        exception.Message.ShouldContain("GraphNotification/GraphEvent");
        exception.Message.ShouldContain(nameof(AddTenTransform));
        exception.Message.ShouldContain("Scoped");
    }

    [Fact]
    public void CompiledMode_WithUnprovableConstructorDependencies_FailsFast()
    {
        // Arrange - nodes are unregistered and take constructor dependencies (the
        // Recorder), so their lifetimes cannot be proven singleton-safe
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddNotifliwyServer(serverBuilder => serverBuilder.AddSector<CompiledParityConfig>());

        using var serviceProvider = services.BuildServiceProvider();

        // Act + Assert
        var exception = Should.Throw<SectorCaptiveDependencyException>(
            () => serviceProvider.GetRequiredService<SectorGraphExecutor<GraphNotification, GraphEvent>>());

        exception.Message.ShouldContain("GraphNotification/GraphEvent");
        exception.Message.ShouldContain(nameof(DoublingMapper));
    }

    [Fact]
    public void AutoMode_WithCompileSafeNodes_SelectsCompiledPath()
    {
        // Arrange - every node singleton-registered: compile-safe
        var services = BuildParityServices<AutoParityConfig>(new Recorder(), ServiceLifetime.Singleton);

        using var serviceProvider = services.BuildServiceProvider();

        // Act
        var executor = serviceProvider.GetRequiredService<SectorGraphExecutor<GraphNotification, GraphEvent>>();

        // Assert
        executor.Decision.Mode.ShouldBe(SectorExecutionMode.Compiled);
        executor.Decision.Reasons.ShouldBeEmpty();
    }

    [Fact]
    public void AutoMode_WithScopedNode_FallsBackToScopedPathWithReason()
    {
        // Arrange - a scoped node blocks compilation; Auto must fall back, not throw
        var services = BuildParityServices<AutoParityConfig>(new Recorder(), ServiceLifetime.Singleton);
        services.AddScoped(typeof(AddTenTransform));

        using var serviceProvider = services.BuildServiceProvider();

        // Act
        var executor = serviceProvider.GetRequiredService<SectorGraphExecutor<GraphNotification, GraphEvent>>();

        // Assert - the decision carries the blocker naming the offending node
        executor.Decision.Mode.ShouldBe(SectorExecutionMode.Scoped);
        executor.Decision.Reasons.ShouldContain(reason => reason.Contains(nameof(AddTenTransform)));
    }
}
