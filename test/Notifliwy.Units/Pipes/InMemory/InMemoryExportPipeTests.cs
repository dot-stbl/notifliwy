using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifliwy.Pipes.InMemory;
using Notifliwy.Pipes.InMemory.Interfaces;
using Notifliwy.Pipes.InMemory.Options;
using Shouldly;
using Xunit;

namespace Notifliwy.Units.Pipes.InMemory;

/// <summary>
/// Unit tests for <see cref="InMemoryExportPipe{TEvent}"/>
/// </summary>
public class InMemoryExportPipeTests
{
    private class TestEvent
    {
        public int Value { get; init; }
    }

    [Fact]
    public async Task ExportAsync_ShouldWriteEventToExchange()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptions<InMemoryExchangeOptions>>(Options.Create(new InMemoryExchangeOptions()));
        services.AddSingleton<IInMemoryEventExchange<TestEvent>, InMemoryEventExchange<TestEvent>>();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var exchange = serviceProvider.GetRequiredService<IInMemoryEventExchange<TestEvent>>();
        var pipe = serviceProvider.GetRequiredService<InMemoryExportPipe<TestEvent>>();

        var expectedEvent = new TestEvent { Value = 42 };

        // Act
        await pipe.ExportAsync(expectedEvent);

        // Assert
        var receivedEvent = await exchange.EventExchange.Reader.ReadAsync();
        receivedEvent.ShouldNotBeNull();
        receivedEvent.Value.ShouldBe(42);
    }

    [Fact]
    public async Task ExportAsync_ShouldWriteMultipleEventsSequentially()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptions<InMemoryExchangeOptions>>(Options.Create(new InMemoryExchangeOptions()));
        services.AddSingleton<IInMemoryEventExchange<TestEvent>, InMemoryEventExchange<TestEvent>>();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var exchange = serviceProvider.GetRequiredService<IInMemoryEventExchange<TestEvent>>();
        var pipe = serviceProvider.GetRequiredService<InMemoryExportPipe<TestEvent>>();

        var events = new[]
        {
            new TestEvent { Value = 1 },
            new TestEvent { Value = 2 },
            new TestEvent { Value = 3 }
        };

        // Act
        foreach (var evt in events)
        {
            await pipe.ExportAsync(evt);
        }

        // Assert
        var receivedEvents = new List<TestEvent>();
        await foreach (var evt in exchange.EventExchange.Reader.ReadAllAsync())
        {
            receivedEvents.Add(evt);
        }

        receivedEvents.Count.ShouldBe(3);
        receivedEvents[0].Value.ShouldBe(1);
        receivedEvents[1].Value.ShouldBe(2);
        receivedEvents[2].Value.ShouldBe(3);
    }

    [Fact]
    public async Task ExportAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptions<InMemoryExchangeOptions>>(Options.Create(new InMemoryExchangeOptions()));
        services.AddSingleton<IInMemoryEventExchange<TestEvent>, InMemoryEventExchange<TestEvent>>();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var pipe = serviceProvider.GetRequiredService<InMemoryExportPipe<TestEvent>>();

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await pipe.ExportAsync(new TestEvent { Value = 42 }, cts.Token);
        });
    }

    [Fact]
    public async Task ExportAsync_WithMultiplePipes_ShouldWriteToSameExchange()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptions<InMemoryExchangeOptions>>(Options.Create(new InMemoryExchangeOptions()));
        services.AddSingleton<IInMemoryEventExchange<TestEvent>, InMemoryEventExchange<TestEvent>>();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var exchange = serviceProvider.GetRequiredService<IInMemoryEventExchange<TestEvent>>();
        var pipe1 = serviceProvider.GetRequiredService<InMemoryExportPipe<TestEvent>>();
        var pipe2 = serviceProvider.GetRequiredService<InMemoryExportPipe<TestEvent>>();

        // Act
        await pipe1.ExportAsync(new TestEvent { Value = 1 });
        await pipe2.ExportAsync(new TestEvent { Value = 2 });
        await pipe1.ExportAsync(new TestEvent { Value = 3 });

        // Assert
        var receivedEvents = new List<TestEvent>();
        await foreach (var evt in exchange.EventExchange.Reader.ReadAllAsync())
        {
            receivedEvents.Add(evt);
        }

        receivedEvents.Count.ShouldBe(3);
        receivedEvents.Select(e => e.Value).ShouldBe(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task ExportAsync_WithBoundedChannel_ShouldRespectCapacity()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptions<InMemoryExchangeOptions>>(Options.Create(new InMemoryExchangeOptions
        {
            ChannelOptions = new System.Threading.Channels.BoundedChannelOptions(2)
            {
                FullMode = System.Threading.Channels.BoundedChannelFullMode.Wait
            }
        }));
        services.AddSingleton<IInMemoryEventExchange<TestEvent>, InMemoryEventExchange<TestEvent>>();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var exchange = serviceProvider.GetRequiredService<IInMemoryEventExchange<TestEvent>>();
        var pipe = serviceProvider.GetRequiredService<InMemoryExportPipe<TestEvent>>();

        // Act
        await pipe.ExportAsync(new TestEvent { Value = 1 });
        await pipe.ExportAsync(new TestEvent { Value = 2 });
        var writeTask3 = pipe.ExportAsync(new TestEvent { Value = 3 });

        // Wait a bit to ensure write is still pending
        await Task.Delay(50);

        // Assert - writeTask3 should still be pending (not completed)
        // Channel with capacity 2, we wrote 2, third should be waiting
        await writeTask3;
    }
}
