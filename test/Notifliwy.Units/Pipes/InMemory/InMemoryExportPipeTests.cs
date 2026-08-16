using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Pipes.InMemory.Interfaces;
using Notifliwy.Pipes.InMemory.Options;
using Notifliwy.Pipes.Interfaces;
using Notifliwy.Units.Helpers;
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
        var serviceProvider = NotifliwyTestProviders.BuildInMemoryProvider();

        var exchange = serviceProvider.GetRequiredService<IInMemoryEventExchange<TestEvent>>();
        var pipe = serviceProvider.GetRequiredService<IExportPipe<TestEvent>>();

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
        var serviceProvider = NotifliwyTestProviders.BuildInMemoryProvider();

        var exchange = serviceProvider.GetRequiredService<IInMemoryEventExchange<TestEvent>>();
        var pipe = serviceProvider.GetRequiredService<IExportPipe<TestEvent>>();

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

        exchange.EventExchange.Writer.Complete();

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
        var serviceProvider = NotifliwyTestProviders.BuildInMemoryProvider();

        var pipe = serviceProvider.GetRequiredService<IExportPipe<TestEvent>>();

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
        var serviceProvider = NotifliwyTestProviders.BuildInMemoryProvider();

        var exchange = serviceProvider.GetRequiredService<IInMemoryEventExchange<TestEvent>>();
        var pipe1 = serviceProvider.GetRequiredService<IExportPipe<TestEvent>>();
        var pipe2 = serviceProvider.GetRequiredService<IExportPipe<TestEvent>>();

        // Act
        await pipe1.ExportAsync(new TestEvent { Value = 1 });
        await pipe2.ExportAsync(new TestEvent { Value = 2 });
        await pipe1.ExportAsync(new TestEvent { Value = 3 });

        exchange.EventExchange.Writer.Complete();

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
        var serviceProvider = NotifliwyTestProviders.BuildInMemoryProvider(options => options
            .ChannelOptions = new BoundedChannelOptions(capacity: 2)
            {
                FullMode = BoundedChannelFullMode.Wait
            });

        var exchange = serviceProvider.GetRequiredService<IInMemoryEventExchange<TestEvent>>();
        var pipe = serviceProvider.GetRequiredService<IExportPipe<TestEvent>>();

        // Act
        await pipe.ExportAsync(new TestEvent { Value = 1 });
        await pipe.ExportAsync(new TestEvent { Value = 2 });
        var pendingWrite = pipe.ExportAsync(new TestEvent { Value = 3 }).AsTask();

        // Assert - channel is full (capacity 2), third write waits for free space
        await Task.Delay(50);
        pendingWrite.IsCompleted.ShouldBeFalse();

        var freedEvent = await exchange.EventExchange.Reader.ReadAsync();
        freedEvent.Value.ShouldBe(1);

        await pendingWrite;
    }
}
