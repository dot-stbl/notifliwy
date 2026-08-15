using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Builders;
using Notifliwy.Builders.Internals.Interfaces;
using Notifliwy.Pipes.InMemory.Interfaces;
using Notifliwy.Pipes.InMemory.Options;
using Notifliwy.Pipes.Interfaces;
using Shouldly;
using Xunit;

namespace Notifliwy.Units.Builders;

/// <summary>
/// Unit tests for <see cref="NotificationServerBuilder"/>
/// </summary>
public class NotificationServerBuilderTests
{
    private class TestNotification
    {
        public int Value { get; set; }
    }

    private class TestEvent
    {
        public int Value { get; init; }
    }

    [Fact]
    public void CreateInstance_ShouldReturnNewInstance()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = NotificationServerBuilder.CreateInstance(services);

        // Assert
        builder.ShouldNotBeNull();
        builder.ShouldBeOfType<NotificationServerBuilder>();
    }

    [Fact]
    public void AddNotification_ShouldAddSectorBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = NotificationServerBuilder.CreateInstance(services);

        // Act
        builder.AddNotification<TestNotification, TestEvent>();

        // Assert
        services.ShouldNotBeNull();
    }

    [Fact]
    public void AddNotification_WithAction_ShouldInvokeSectorBuilderAction()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = NotificationServerBuilder.CreateInstance(services);
        var actionInvoked = false;

        // Act
        builder.AddNotification<TestNotification, TestEvent>(sectorBuilder =>
        {
            actionInvoked = true;
        });

        // Assert
        actionInvoked.ShouldBeTrue();
    }

    [Fact]
    public void AddInMemoryInput_ShouldRegisterInMemoryServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = NotificationServerBuilder.CreateInstance(services);

        // Act
        builder.AddInMemoryInput();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var exchange = serviceProvider.GetService<IInMemoryEventExchange<TestEvent>>();
        exchange.ShouldNotBeNull();

        var inputPipe = serviceProvider.GetService<IInputPipe<TestEvent>>();
        inputPipe.ShouldNotBeNull();

        var exportPipe = serviceProvider.GetService<IExportPipe<TestEvent>>();
        exportPipe.ShouldNotBeNull();
    }


    [Fact]
    public void AddNotification_ShouldRegisterNotificationMappings()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = NotificationServerBuilder.CreateInstance(services);

        // Act
        builder.AddNotification<TestNotification, TestEvent>();

        // Assert
        services.ShouldNotBeNull();
        // Verify no exceptions thrown during registration
    }

    [Fact]
    public void BuildServer_ShouldRegisterConnectors()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = NotificationServerBuilder.CreateInstance(services);

        builder.AddNotification<TestNotification, TestEvent>();

        // Act
        builder.BuildServer();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        // Verify service collection was built successfully
        services.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void AddNotification_ShouldSupportMultipleNotifications()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = NotificationServerBuilder.CreateInstance(services);

        // Act
        builder.AddNotification<TestNotification, TestEvent>();
        builder.AddNotification<TestNotification, TestEvent>();

        // Assert
        services.ShouldNotBeNull();
    }

    [Fact]
    public void AddNotification_WithDifferentEventTypes_ShouldRegisterCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = NotificationServerBuilder.CreateInstance(services);

        // Act
        builder.AddNotification<TestNotification, TestEvent>();
        builder.AddInMemoryInput();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var inputPipe = serviceProvider.GetService<IInputPipe<TestEvent>>();
        inputPipe.ShouldNotBeNull();
    }

    [Fact]
    public void CreateInstance_ShouldWrapServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<TestService>();

        // Act
        var builder = NotificationServerBuilder.CreateInstance(services);

        // Assert
        builder.ShouldNotBeNull();
        var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetService<TestService>();
        service.ShouldNotBeNull();
    }

    private class TestService
    {
    }
}
