using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Builders.Internals;
using Notifliwy.Connectors;
using Shouldly;
using Xunit;

namespace Notifliwy.Units.Builders;

/// <summary>
/// Unit tests for <see cref="ConnectorsBuilder{TEvent}"/>
/// </summary>
public class ConnectorsBuilderTests
{
    private class TestEvent
    {
        public int Value { get; init; }
    }

    [Fact]
    public void BuildConnector_ShouldRegisterNotificationConnector()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new ConnectorsBuilder<TestEvent>();

        // Act
        builder.BuildConnector(services);

        // Assert
        services.Count(d =>
            d.ImplementationType == typeof(NotificationConnector<TestEvent>)).ShouldBe(1);
    }

    [Fact]
    public void BuildConnector_ShouldNotRegisterNotificationConnector_WhenAlreadyRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder1 = new ConnectorsBuilder<TestEvent>();
        var builder2 = new ConnectorsBuilder<TestEvent>();

        // Act
        builder1.BuildConnector(services);
        var initialCount = services.Count(d =>
            d.ImplementationType == typeof(NotificationConnector<TestEvent>));

        builder2.BuildConnector(services);
        var finalCount = services.Count(d =>
            d.ImplementationType == typeof(NotificationConnector<TestEvent>));

        // Assert
        initialCount.ShouldBe(1);
        finalCount.ShouldBe(1);
    }
}
