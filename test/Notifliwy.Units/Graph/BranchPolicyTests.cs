using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Graph;
using Notifliwy.Graph.Internals;
using Notifliwy.Join.Interfaces;
using Notifliwy.Transform.Interfaces;
using Shouldly;
using Xunit;

namespace Notifliwy.Units.Graph;

/// <summary>
/// Unit tests for <see cref="BranchPolicy"/> behaviour: FailFast rethrow vs BestEffort continue.
/// </summary>
public class BranchPolicyTests
{
    private sealed class ThrowingExporter : INotificationExporter<GraphNotification>
    {
        public ValueTask ThrowAsync(GraphNotification notification, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("email branch failed");
        }
    }

    private sealed class SlackExporter(Recorder recorder) : INotificationExporter<GraphNotification>
    {
        public ValueTask ThrowAsync(GraphNotification notification, CancellationToken cancellationToken = default)
        {
            recorder.Record($"slack:{notification.Value}");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class AddTenTransform : INotificationTransform<GraphNotification>
    {
        public ValueTask<GraphNotification> TransformAsync(GraphNotification notification, CancellationToken cancellationToken = default)
        {
            // branches receive the same notification instance — return a new one instead of mutating
            return ValueTask.FromResult(new GraphNotification { Value = notification.Value + 10 });
        }
    }

    private sealed class RecordingJoin(Recorder recorder) : INotificationJoin<GraphNotification>
    {
        public ValueTask<GraphNotification> JoinAsync(IReadOnlyList<GraphNotification> notifications, CancellationToken cancellationToken = default)
        {
            recorder.Record($"join-count:{notifications.Count}");
            return ValueTask.FromResult(notifications[0]);
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
    public async Task FailFast_Default_RethrowsAfterAllBranchesObserved()
    {
        var recorder = new Recorder();
        var (provider, executor) = GraphTestHost.Build<GraphNotification, GraphEvent>(
            graph => graph
                .Map((inputEvent, cancellationToken) => ValueTask.FromResult(new GraphNotification { Value = inputEvent.Value }))
                .Branch(
                    branch => branch.Export<ThrowingExporter>(),
                    branch => branch.Export<SlackExporter>()),
            services => services.AddSingleton(recorder));

        await using (provider)
        {
            var exception = await Should.ThrowAsync<InvalidOperationException>(
                async () => await executor.ExecuteAsync(new GraphEvent { Value = 1 }));

            exception.Message.ShouldBe("email branch failed");

            // WhenAll observed the surviving branch before rethrowing
            recorder.Calls.ShouldContain("slack:1");
        }
    }

    [Fact]
    public async Task FailFast_Explicit_RethrowsBranchFault()
    {
        var (provider, executor) = GraphTestHost.Build<GraphNotification, GraphEvent>(
            graph => graph
                .Map((inputEvent, cancellationToken) => ValueTask.FromResult(new GraphNotification { Value = inputEvent.Value }))
                .Branch(
                    BranchPolicy.FailFast,
                    branch => branch.Export<ThrowingExporter>(),
                    branch => branch.Export<ThrowingExporter>()));

        await using (provider)
        {
            await Should.ThrowAsync<InvalidOperationException>(
                async () => await executor.ExecuteAsync(new GraphEvent { Value = 1 }));
        }
    }

    [Fact]
    public async Task BestEffort_ContinuesWithSurvivingBranch()
    {
        var recorder = new Recorder();
        var (provider, executor) = GraphTestHost.Build<GraphNotification, GraphEvent>(
            graph => graph
                .Map((inputEvent, cancellationToken) => ValueTask.FromResult(new GraphNotification { Value = inputEvent.Value }))
                .Branch(
                    BranchPolicy.BestEffort,
                    branch => branch.Export<ThrowingExporter>(),
                    branch => branch.Transform<AddTenTransform>().Export<SlackExporter>())
                .Join<RecordingJoin>()
                .Export<FinalExporter>(),
            services => services.AddSingleton(recorder));

        await using (provider)
        {
            await executor.ExecuteAsync(new GraphEvent { Value = 1 });

            // survivor continued; join saw only the survivor (single output → passthrough);
            // downstream export received the survivor notification
            recorder.Calls.ShouldContain("slack:11");
            recorder.Calls.ShouldNotContain("join-count:2");
            recorder.Calls.ShouldContain("final:11");
        }
    }

    [Fact]
    public async Task BestEffort_MultiBranchJoin_ReducedFromSurvivorsOnly()
    {
        var recorder = new Recorder();
        var (provider, executor) = GraphTestHost.Build<GraphNotification, GraphEvent>(
            graph => graph
                .Map((inputEvent, cancellationToken) => ValueTask.FromResult(new GraphNotification { Value = inputEvent.Value }))
                .Branch(
                    BranchPolicy.BestEffort,
                    branch => branch.Export<ThrowingExporter>(),
                    branch => branch.Transform<AddTenTransform>().Export<SlackExporter>(),
                    branch => branch.Export<SlackExporter>())
                .Join<RecordingJoin>()
                .Export<FinalExporter>(),
            services => services.AddSingleton(recorder));

        await using (provider)
        {
            await executor.ExecuteAsync(new GraphEvent { Value = 1 });

            // two survivors reached the reducer, the failed branch was dropped
            recorder.Calls.ShouldContain("join-count:2");
            recorder.Calls.ShouldContain("final:11");
        }
    }

    [Fact]
    public async Task BestEffort_FailureIsLogged()
    {
        var spyLogger = new SpyLogger<SectorGraphExecutor<GraphNotification, GraphEvent>>();
        var (provider, executor) = GraphTestHost.Build<GraphNotification, GraphEvent>(
            graph => graph
                .Map((inputEvent, cancellationToken) => ValueTask.FromResult(new GraphNotification { Value = inputEvent.Value }))
                .Branch(
                    BranchPolicy.BestEffort,
                    branch => branch.Export<ThrowingExporter>(),
                    branch => branch.Export<ThrowingExporter>()),
            services => services.AddSingleton<ILogger<SectorGraphExecutor<GraphNotification, GraphEvent>>>(spyLogger));

        await using (provider)
        {
            await executor.ExecuteAsync(new GraphEvent { Value = 1 });

            spyLogger.Entries.ShouldContain(entry => entry.Level == LogLevel.Error);
        }
    }
}
