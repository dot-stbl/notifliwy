using System;
using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Exceptions;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Join.Interfaces;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Transform.Interfaces;
using Shouldly;
using Xunit;

namespace Notifliwy.Units.Graph;

/// <summary>
/// Unit tests for sector graph structure validation at plan build time.
/// </summary>
public class GraphValidationTests
{
    private sealed class AnyCondition : INotificationCondition<GraphNotification, GraphEvent>
    {
        public ValueTask<bool> AllowItAsync(GraphEvent inputEvent, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(true);
        }
    }

    private sealed class AnyMapper : INotificationMapper<GraphNotification, GraphEvent>
    {
        public ValueTask<GraphNotification> ConvertAsync(GraphEvent inputEvent, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new GraphNotification { Value = inputEvent.Value });
        }
    }

    private sealed class AnyTransform : INotificationTransform<GraphNotification>
    {
        public ValueTask<GraphNotification> TransformAsync(GraphNotification notification, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(notification);
        }
    }

    private sealed class AnyExporter : INotificationExporter<GraphNotification>
    {
        public ValueTask ThrowAsync(GraphNotification notification, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class AnyJoin : INotificationJoin<GraphNotification>
    {
        public ValueTask<GraphNotification> JoinAsync(System.Collections.Generic.IReadOnlyList<GraphNotification> notifications, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(notifications[0]);
        }
    }

    private static SectorGraphValidationException BuildShouldThrow(
        Action<Notifliwy.Graph.SectorGraphBuilder<GraphNotification, GraphEvent>> configure)
    {
        var graphBuilder = new Notifliwy.Graph.SectorGraphBuilder<GraphNotification, GraphEvent>();
        configure(graphBuilder);

        return Should.Throw<SectorGraphValidationException>(() => graphBuilder.BuildPlan());
    }

    [Fact]
    public void Map_Missing_IsRegistrationError()
    {
        var exception = BuildShouldThrow(graph => graph
            .When<AnyCondition>()
            .Export<AnyExporter>());

        exception.Message.ShouldContain("GraphNotification/GraphEvent");
        exception.Message.ShouldContain("Map node is required exactly once: none registered");
    }

    [Fact]
    public void Map_AfterTransform_IsRegistrationError()
    {
        var exception = BuildShouldThrow(graph => graph
            .Transform<AnyTransform>()
            .Map<AnyMapper>());

        exception.Message.ShouldContain("Map must be registered before Transform");
    }

    [Fact]
    public void Map_RegisteredTwice_IsRegistrationError()
    {
        var exception = BuildShouldThrow(graph => graph
            .Map<AnyMapper>()
            .Map<AnyMapper>());

        exception.Message.ShouldContain("Map node is required exactly once: registered 2 times");
    }

    [Fact]
    public void When_AfterMap_IsRegistrationError()
    {
        var exception = BuildShouldThrow(graph => graph
            .Map<AnyMapper>()
            .When<AnyCondition>());

        exception.Message.ShouldContain("When must be registered before Map");
    }

    [Fact]
    public void Branch_DeadEndBranch_IsRegistrationError()
    {
        var exception = BuildShouldThrow(graph => graph
            .Map<AnyMapper>()
            .Branch(
                branch => branch.Export<AnyExporter>(),
                branch => branch.Transform<AnyTransform>()));

        exception.Message.ShouldContain("Branch sub-graph must contain at least one Export node (dead-end branch)");
    }

    [Fact]
    public void Branch_Empty_IsRegistrationError()
    {
        var exception = BuildShouldThrow(graph => graph
            .Map<AnyMapper>()
            .Branch());

        exception.Message.ShouldContain("Branch requires at least one branch sub-graph");
    }

    [Fact]
    public void Join_WithoutBranch_IsRegistrationError()
    {
        var exception = BuildShouldThrow(graph => graph
            .Map<AnyMapper>()
            .Join<AnyJoin>());

        exception.Message.ShouldContain("Join is only valid after a Branch node");
    }

    [Fact]
    public void Join_TwiceAfterSingleBranch_IsRegistrationError()
    {
        var exception = BuildShouldThrow(graph => graph
            .Map<AnyMapper>()
            .Branch(branch => branch.Export<AnyExporter>())
            .Join<AnyJoin>()
            .Join<AnyJoin>());

        exception.Message.ShouldContain("Join is only valid after a Branch node");
    }

    [Fact]
    public void When_InsideBranch_IsRegistrationError()
    {
        var exception = BuildShouldThrow(graph => graph
            .Map<AnyMapper>()
            .Branch(branch => branch
                .When<AnyCondition>()
                .Export<AnyExporter>()));

        exception.Message.ShouldContain("When is not allowed inside a branch sub-graph");
    }

    [Fact]
    public void Map_InsideBranch_IsRegistrationError()
    {
        var exception = BuildShouldThrow(graph => graph
            .Map<AnyMapper>()
            .Branch(branch => branch
                .Map<AnyMapper>()
                .Export<AnyExporter>()));

        exception.Message.ShouldContain("Map is not allowed inside a branch sub-graph");
    }

    [Fact]
    public void NestedBranch_DeadEndSubBranch_IsRegistrationError()
    {
        var exception = BuildShouldThrow(graph => graph
            .Map<AnyMapper>()
            .Branch(branch => branch
                .Branch(
                    nested => nested.Export<AnyExporter>(),
                    nested => nested.Transform<AnyTransform>())
                .Export<AnyExporter>()));

        exception.Message.ShouldContain("Branch sub-graph must contain at least one Export node (dead-end branch)");
    }

    [Fact]
    public void ValidGraph_BuildsPlanWithoutThrowing()
    {
        var graphBuilder = new Notifliwy.Graph.SectorGraphBuilder<GraphNotification, GraphEvent>();

        graphBuilder
            .When<AnyCondition>()
            .Map<AnyMapper>()
            .Transform<AnyTransform>()
            .Branch(
                branch => branch.Export<AnyExporter>(),
                branch => branch.Transform<AnyTransform>().Export<AnyExporter>())
            .Join<AnyJoin>()
            .Export<AnyExporter>();

        var plan = graphBuilder.BuildPlan();

        plan.ShouldNotBeNull();
    }
}
