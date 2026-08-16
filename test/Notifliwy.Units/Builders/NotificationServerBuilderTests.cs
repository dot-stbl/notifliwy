using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notifliwy.Builders;
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
    public void AddInMemoryInput_ShouldRegisterOptionsInfrastructure()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = NotificationServerBuilder.CreateInstance(services);

        // Act
        builder.AddInMemoryInput();
        var serviceProvider = services.BuildServiceProvider();

        // Assert - bare collection resolves IOptions without activation failure (GH #11)
        var options = serviceProvider.GetService<IOptions<InMemoryExchangeOptions>>();
        options.ShouldNotBeNull();
        options.Value.ShouldNotBeNull();
    }

    [Fact]
    public void AddInMemoryInput_WithConfigure_ShouldBindConfiguredOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = NotificationServerBuilder.CreateInstance(services);

        // Act
        builder.AddInMemoryInput(options => options.ChannelOptions =
            new System.Threading.Channels.BoundedChannelOptions(capacity: 2)
            {
                FullMode = System.Threading.Channels.BoundedChannelFullMode.Wait
            });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetRequiredService<IOptions<InMemoryExchangeOptions>>();
        var boundedOptions = options.Value.ChannelOptions
            .ShouldBeOfType<System.Threading.Channels.BoundedChannelOptions>();
        boundedOptions.Capacity.ShouldBe(2);
        boundedOptions.FullMode.ShouldBe(System.Threading.Channels.BoundedChannelFullMode.Wait);
    }

    [Fact]
    public async Task AddInMemoryInput_WithConfigure_ShouldApplyConfiguredCapacityToExchange()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = NotificationServerBuilder.CreateInstance(services);

        // Act
        builder.AddInMemoryInput(options => options.ChannelOptions =
            new System.Threading.Channels.BoundedChannelOptions(capacity: 3)
            {
                FullMode = System.Threading.Channels.BoundedChannelFullMode.Wait
            });
        var serviceProvider = services.BuildServiceProvider();

        var exchange = serviceProvider.GetRequiredService<IInMemoryEventExchange<TestEvent>>();

        // Assert - capacity 3 is observed: three writes fit, the fourth waits
        await exchange.EventExchange.Writer.WriteAsync(new TestEvent { Value = 1 });
        await exchange.EventExchange.Writer.WriteAsync(new TestEvent { Value = 2 });
        await exchange.EventExchange.Writer.WriteAsync(new TestEvent { Value = 3 });
        var pendingWrite = exchange.EventExchange.Writer
            .WriteAsync(new TestEvent { Value = 4 }).AsTask();

        await Task.Delay(50);
        pendingWrite.IsCompleted.ShouldBeFalse();

        var freedEvent = await exchange.EventExchange.Reader.ReadAsync();
        freedEvent.Value.ShouldBe(1);

        await pendingWrite;
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
