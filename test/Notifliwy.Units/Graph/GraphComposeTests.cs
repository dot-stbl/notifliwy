using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Join.Interfaces;
using Notifliwy.Transform.Interfaces;
using Shouldly;
using Xunit;

namespace Notifliwy.Units.Graph;

/// <summary>
/// Unit tests for graph composition: execution order, branch fan-out, join reduction.
/// </summary>
public class GraphComposeTests
{
    private sealed class RecordingTransform(Recorder recorder) : INotificationTransform<GraphNotification>
    {
        public ValueTask<GraphNotification> TransformAsync(GraphNotification notification, CancellationToken cancellationToken = default)
        {
            recorder.Record($"transform:{notification.Value}");
            return ValueTask.FromResult(notification);
        }
    }

    private sealed class AddTenBranchTransform(Recorder recorder) : INotificationTransform<GraphNotification>
    {
        public ValueTask<GraphNotification> TransformAsync(GraphNotification notification, CancellationToken cancellationToken = default)
        {
            recorder.Record("branch-a");

            // branches receive the same notification instance — return a new one instead of mutating
            return ValueTask.FromResult(new GraphNotification { Value = notification.Value + 10 });
        }
    }

    private sealed class AddTwentyBranchTransform(Recorder recorder) : INotificationTransform<GraphNotification>
    {
        public ValueTask<GraphNotification> TransformAsync(GraphNotification notification, CancellationToken cancellationToken = default)
        {
            recorder.Record("branch-b");

            // branches receive the same notification instance — return a new one instead of mutating
            return ValueTask.FromResult(new GraphNotification { Value = notification.Value + 20 });
        }
    }

    private sealed class SumJoin(Recorder recorder) : INotificationJoin<GraphNotification>
    {
        public ValueTask<GraphNotification> JoinAsync(IReadOnlyList<GraphNotification> notifications, CancellationToken cancellationToken = default)
        {
            recorder.Record($"join:[{string.Join(",", notifications.Select(notification => notification.Value))}]");
            return ValueTask.FromResult(new GraphNotification { Value = notifications.Sum(notification => notification.Value) });
        }
    }

    private sealed class NeverCalledJoin(Recorder recorder) : INotificationJoin<GraphNotification>
    {
        public ValueTask<GraphNotification> JoinAsync(IReadOnlyList<GraphNotification> notifications, CancellationToken cancellationToken = default)
        {
            recorder.Record("join-called");
            return ValueTask.FromResult(notifications[0]);
        }
    }

    private sealed class StampExporter(Recorder recorder) : INotificationExporter<GraphNotification>
    {
        public ValueTask ThrowAsync(GraphNotification notification, CancellationToken cancellationToken = default)
        {
            recorder.Record($"export:{notification.Value}");
            return ValueTask.CompletedTask;
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

    [Fact]
    public async Task Compose_NodesRunInRegistrationOrder()
    {
        var recorder = new Recorder();
        var (provider, executor) = GraphTestHost.Build<GraphNotification, GraphEvent>(
            graph => graph
                .Map((inputEvent, cancellationToken) =>
                {
                    recorder.Record("map");
                    return ValueTask.FromResult(new GraphNotification { Value = inputEvent.Value });
                })
                .Transform<RecordingTransform>()
                .Custom((notification, cancellationToken) =>
                {
                    recorder.Record("custom");
                    return ValueTask.FromResult(notification);
                })
                .Export<StampExporter>(),
            services => services.AddSingleton(recorder));

        await using (provider)
        {
            await executor.ExecuteAsync(new GraphEvent { Value = 1 });

            recorder.Calls.ShouldBe(["map", "transform:1", "custom", "export:1"]);
        }
    }

    [Fact]
    public async Task Branch_BothBranchesRunWithSameInput()
    {
        var recorder = new Recorder();
        var branchAValues = new List<int>();
        var branchBValues = new List<int>();
        var (provider, executor) = GraphTestHost.Build<GraphNotification, GraphEvent>(
            graph => graph
                .Map((inputEvent, cancellationToken) => ValueTask.FromResult(new GraphNotification { Value = inputEvent.Value }))
                .Branch(
                    branch => branch
                        .Transform<AddTenBranchTransform>()
                        .Custom((notification, cancellationToken) =>
                        {
                            branchAValues.Add(notification.Value);
                            return ValueTask.FromResult(notification);
                        })
                        .Export<StampExporter>(),
                    branch => branch
                        .Transform<AddTwentyBranchTransform>()
                        .Custom((notification, cancellationToken) =>
                        {
                            branchBValues.Add(notification.Value);
                            return ValueTask.FromResult(notification);
                        })
                        .Export<StampExporter>()),
            services => services.AddSingleton(recorder));

        await using (provider)
        {
            await executor.ExecuteAsync(new GraphEvent { Value = 5 });

            // both branches saw the same mapped input (5) and ran their transforms
            branchAValues.ShouldBe([15]);
            branchBValues.ShouldBe([25]);
            recorder.Calls.ShouldContain("branch-a");
            recorder.Calls.ShouldContain("branch-b");
            recorder.Calls.Count(call => call.StartsWith("export:")).ShouldBe(2);
        }
    }

    [Fact]
    public async Task Join_ReceivesBothOutputsAndReducesDownstream()
    {
        var recorder = new Recorder();
        var (provider, executor) = GraphTestHost.Build<GraphNotification, GraphEvent>(
            graph => graph
                .Map((inputEvent, cancellationToken) => ValueTask.FromResult(new GraphNotification { Value = inputEvent.Value }))
                .Branch(
                    branch => branch.Transform<AddTenBranchTransform>().Export<StampExporter>(),
                    branch => branch.Transform<AddTwentyBranchTransform>().Export<StampExporter>())
                .Join<SumJoin>()
                .Export<FinalExporter>(),
            services => services.AddSingleton(recorder));

        await using (provider)
        {
            await executor.ExecuteAsync(new GraphEvent { Value = 5 });

            // reducer received [15, 25] and the reduced value flowed downstream
            recorder.Calls.ShouldContain("join:[15,25]");
            recorder.Calls.ShouldContain("final:40");
        }
    }

    [Fact]
    public async Task Join_SingleBranch_IsPassthroughWithoutReducer()
    {
        var recorder = new Recorder();
        var (provider, executor) = GraphTestHost.Build<GraphNotification, GraphEvent>(
            graph => graph
                .Map((inputEvent, cancellationToken) => ValueTask.FromResult(new GraphNotification { Value = inputEvent.Value }))
                .Branch(
                    branch => branch.Transform<AddTenBranchTransform>().Export<StampExporter>())
                .Join<NeverCalledJoin>()
                .Export<FinalExporter>(),
            services => services.AddSingleton(recorder));

        await using (provider)
        {
            await executor.ExecuteAsync(new GraphEvent { Value = 5 });

            // single survivor flows straight through; reducer not invoked
            recorder.Calls.ShouldNotContain("join-called");
            recorder.Calls.ShouldContain("final:15");
        }
    }

    [Fact]
    public async Task Transform_AfterJoin_ReceivesReducedNotification()
    {
        var recorder = new Recorder();
        var (provider, executor) = GraphTestHost.Build<GraphNotification, GraphEvent>(
            graph => graph
                .Map((inputEvent, cancellationToken) => ValueTask.FromResult(new GraphNotification { Value = inputEvent.Value }))
                .Branch(
                    branch => branch.Transform<AddTenBranchTransform>().Export<StampExporter>(),
                    branch => branch.Transform<AddTwentyBranchTransform>().Export<StampExporter>())
                .Join<SumJoin>()
                .Transform<RecordingTransform>()
                .Export<FinalExporter>(),
            services => services.AddSingleton(recorder));

        await using (provider)
        {
            await executor.ExecuteAsync(new GraphEvent { Value = 5 });

            recorder.Calls.ShouldContain("transform:40");
            recorder.Calls.ShouldContain("final:40");
        }
    }
}
