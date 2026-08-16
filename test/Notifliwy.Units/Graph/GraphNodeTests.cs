using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Custom.Interfaces;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Transform.Interfaces;
using Shouldly;
using Xunit;

namespace Notifliwy.Units.Graph;

/// <summary>
/// Unit tests for each sector graph node type behaviour.
/// </summary>
public class GraphNodeTests
{
    private sealed class AllowCondition : INotificationCondition<GraphNotification, GraphEvent>
    {
        public ValueTask<bool> AllowItAsync(GraphEvent inputEvent, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(inputEvent.Value > 0);
        }
    }

    private sealed class DenyCondition : INotificationCondition<GraphNotification, GraphEvent>
    {
        public ValueTask<bool> AllowItAsync(GraphEvent inputEvent, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(false);
        }
    }

    private sealed class ClassMapper : INotificationMapper<GraphNotification, GraphEvent>
    {
        public ValueTask<GraphNotification> ConvertAsync(GraphEvent inputEvent, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new GraphNotification { Value = inputEvent.Value * 2 });
        }
    }

    private sealed class DoubleTransform : INotificationTransform<GraphNotification>
    {
        public ValueTask<GraphNotification> TransformAsync(GraphNotification notification, CancellationToken cancellationToken = default)
        {
            notification.Value *= 2;
            return ValueTask.FromResult(notification);
        }
    }

    private sealed class IncrementTransform : INotificationTransform<GraphNotification>
    {
        public ValueTask<GraphNotification> TransformAsync(GraphNotification notification, CancellationToken cancellationToken = default)
        {
            notification.Value += 1;
            return ValueTask.FromResult(notification);
        }
    }

    private sealed class RateLimitGate : INotificationCustom<GraphNotification>
    {
        public RateLimitGate(Recorder recorder)
        {
            Recorder = recorder;
        }

        private Recorder Recorder { get; }

        public ValueTask<GraphNotification> InvokeAsync(GraphNotification notification, CancellationToken cancellationToken = default)
        {
            Recorder.Record("custom-class");
            notification.Value += 100;
            return ValueTask.FromResult(notification);
        }
    }

    private sealed class CollectionExporter(Recorder recorder) : INotificationExporter<GraphNotification>
    {
        public ValueTask ThrowAsync(GraphNotification notification, CancellationToken cancellationToken = default)
        {
            recorder.Record($"export:{notification.Value}");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class MapperCallRecorder
    {
        public int Calls { get; set; }
    }

    private sealed class RecordingMapper(MapperCallRecorder callRecorder) : INotificationMapper<GraphNotification, GraphEvent>
    {
        public ValueTask<GraphNotification> ConvertAsync(GraphEvent inputEvent, CancellationToken cancellationToken = default)
        {
            callRecorder.Calls++;
            return ValueTask.FromResult(new GraphNotification { Value = inputEvent.Value });
        }
    }

    [Fact]
    public async Task When_Allowed_ContinuesToMapAndExport()
    {
        var recorder = new Recorder();
        var (provider, executor) = GraphTestHost.Build<GraphNotification, GraphEvent>(
            graph => graph
                .When<AllowCondition>()
                .Map<ClassMapper>()
                .Export<CollectionExporter>(),
            services => services.AddSingleton(recorder));

        await using (provider)
        {
            await executor.ExecuteAsync(new GraphEvent { Value = 21 });

            recorder.Calls.ShouldBe(["export:42"]);
        }
    }

    [Fact]
    public async Task When_Denied_StopsBeforeMapAndExport()
    {
        var recorder = new Recorder();
        var callRecorder = new MapperCallRecorder();
        var (provider, executor) = GraphTestHost.Build<GraphNotification, GraphEvent>(
            graph => graph
                .When<DenyCondition>()
                .Map<RecordingMapper>()
                .Export<CollectionExporter>(),
            services =>
            {
                services.AddSingleton(recorder);
                services.AddSingleton(callRecorder);
            });

        await using (provider)
        {
            await executor.ExecuteAsync(new GraphEvent { Value = 21 });

            callRecorder.Calls.ShouldBe(0);
            recorder.Calls.ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task Map_Class_ConvertsEventToNotification()
    {
        var recorder = new Recorder();
        var (provider, executor) = GraphTestHost.Build<GraphNotification, GraphEvent>(
            graph => graph
                .Map<ClassMapper>()
                .Export<CollectionExporter>(),
            services => services.AddSingleton(recorder));

        await using (provider)
        {
            await executor.ExecuteAsync(new GraphEvent { Value = 10 });

            recorder.Calls.ShouldBe(["export:20"]);
        }
    }

    [Fact]
    public async Task Map_Lambda_ConvertsEventToNotification()
    {
        var recorder = new Recorder();
        var (provider, executor) = GraphTestHost.Build<GraphNotification, GraphEvent>(
            graph => graph
                .Map((inputEvent, cancellationToken) => ValueTask.FromResult(new GraphNotification { Value = inputEvent.Value + 1 }))
                .Export<CollectionExporter>(),
            services => services.AddSingleton(recorder));

        await using (provider)
        {
            await executor.ExecuteAsync(new GraphEvent { Value = 41 });

            recorder.Calls.ShouldBe(["export:42"]);
        }
    }

    [Fact]
    public async Task Transform_SecondReceivesFirstOutput()
    {
        var recorder = new Recorder();
        var (provider, executor) = GraphTestHost.Build<GraphNotification, GraphEvent>(
            graph => graph
                .Map((inputEvent, cancellationToken) => ValueTask.FromResult(new GraphNotification { Value = inputEvent.Value }))
                .Transform<DoubleTransform>()
                .Transform<IncrementTransform>()
                .Export<CollectionExporter>(),
            services => services.AddSingleton(recorder));

        await using (provider)
        {
            await executor.ExecuteAsync(new GraphEvent { Value = 10 });

            // (10 * 2) + 1
            recorder.Calls.ShouldBe(["export:21"]);
        }
    }

    [Fact]
    public async Task Export_ReceivesCurrentNotification()
    {
        var recorder = new Recorder();
        var (provider, executor) = GraphTestHost.Build<GraphNotification, GraphEvent>(
            graph => graph
                .Map((inputEvent, cancellationToken) => ValueTask.FromResult(new GraphNotification { Value = inputEvent.Value }))
                .Transform<DoubleTransform>()
                .Export<CollectionExporter>(),
            services => services.AddSingleton(recorder));

        await using (provider)
        {
            await executor.ExecuteAsync(new GraphEvent { Value = 7 });

            recorder.Calls.ShouldBe(["export:14"]);
        }
    }

    [Fact]
    public async Task Custom_Lambda_WrapsInvocationIntoNode()
    {
        var recorder = new Recorder();
        var (provider, executor) = GraphTestHost.Build<GraphNotification, GraphEvent>(
            graph => graph
                .Map((inputEvent, cancellationToken) => ValueTask.FromResult(new GraphNotification { Value = inputEvent.Value }))
                .Custom((notification, cancellationToken) =>
                {
                    recorder.Record("custom-lambda");
                    notification.Value *= 3;
                    return ValueTask.FromResult(notification);
                })
                .Export<CollectionExporter>(),
            services => services.AddSingleton(recorder));

        await using (provider)
        {
            await executor.ExecuteAsync(new GraphEvent { Value = 5 });

            recorder.Calls.ShouldBe(["custom-lambda", "export:15"]);
        }
    }

    [Fact]
    public async Task Custom_Class_InvokedThroughDi()
    {
        var recorder = new Recorder();
        var (provider, executor) = GraphTestHost.Build<GraphNotification, GraphEvent>(
            graph => graph
                .Map((inputEvent, cancellationToken) => ValueTask.FromResult(new GraphNotification { Value = inputEvent.Value }))
                .Custom<RateLimitGate>()
                .Export<CollectionExporter>(),
            services => services.AddSingleton(recorder));

        await using (provider)
        {
            await executor.ExecuteAsync(new GraphEvent { Value = 5 });

            recorder.Calls.ShouldBe(["custom-class", "export:105"]);
        }
    }
}
