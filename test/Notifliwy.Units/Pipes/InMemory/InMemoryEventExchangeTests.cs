using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notifliwy.Pipes.InMemory;
using Notifliwy.Pipes.InMemory.Interfaces;
using Notifliwy.Pipes.InMemory.Options;
using Shouldly;
using Xunit;

namespace Notifliwy.Units.Pipes.InMemory;

/// <summary>
/// Unit tests for <see cref="InMemoryEventExchange{TEvent}"/>
/// </summary>
public class InMemoryEventExchangeTests
{
    private class TestEvent
    {
        public int Value { get; init; }
    }

    [Fact]
    public void Constructor_ShouldCreateExchange()
    {
        // Arrange & Act
        var exchange = new InMemoryEventExchange<TestEvent>(exchangeOptions: null);

        // Assert
        exchange.ShouldNotBeNull();
        exchange.EventExchange.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithOptions_ShouldCreateExchangeWithOptions()
    {
        // Arrange
        var options = Options.Create(new InMemoryExchangeOptions
        {
            ChannelOptions = new System.Threading.Channels.BoundedChannelOptions(100)
            {
                FullMode = System.Threading.Channels.BoundedChannelFullMode.Wait
            }
        });

        // Act
        var exchange = new InMemoryEventExchange<TestEvent>(options);

        // Assert
        exchange.ShouldNotBeNull();
        exchange.EventExchange.ShouldNotBeNull();
    }

    [Fact]
    public async Task WriteAsync_ShouldWriteEventToChannel()
    {
        // Arrange
        var exchange = new InMemoryEventExchange<TestEvent>(exchangeOptions: null);
        var expectedEvent = new TestEvent { Value = 42 };

        // Act
        await exchange.EventExchange.Writer.WriteAsync(expectedEvent);
        var receivedEvent = await exchange.EventExchange.Reader.ReadAsync();

        // Assert
        receivedEvent.ShouldNotBeNull();
        receivedEvent.Value.ShouldBe(42);
    }

    [Fact]
    public async Task WriteAsync_ShouldWriteMultipleEvents()
    {
        // Arrange
        var exchange = new InMemoryEventExchange<TestEvent>(exchangeOptions: null);
        var events = new[]
        {
            new TestEvent { Value = 1 },
            new TestEvent { Value = 2 },
            new TestEvent { Value = 3 }
        };

        // Act
        foreach (var evt in events)
        {
            await exchange.EventExchange.Writer.WriteAsync(evt);
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
    public async Task WriteAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        var exchange = new InMemoryEventExchange<TestEvent>(exchangeOptions: null);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await exchange.EventExchange.Writer.WriteAsync(new TestEvent { Value = 42 }, cts.Token);
        });
    }

    [Fact]
    public async Task WriteAsync_WithBoundedChannel_ShouldWaitWhenFull()
    {
        // Arrange
        var options = Options.Create(new InMemoryExchangeOptions
        {
            ChannelOptions = new System.Threading.Channels.BoundedChannelOptions(2)
            {
                FullMode = System.Threading.Channels.BoundedChannelFullMode.Wait
            }
        });
        var exchange = new InMemoryEventExchange<TestEvent>(options);

        // Act
        await exchange.EventExchange.Writer.WriteAsync(new TestEvent { Value = 1 });
        await exchange.EventExchange.Writer.WriteAsync(new TestEvent { Value = 2 });
        var pendingWrite = exchange.EventExchange.Writer.WriteAsync(new TestEvent { Value = 3 }).AsTask();

        // Assert
        pendingWrite.Status.ShouldBe(TaskStatus.WaitingForActivation);

        // Clean up
        await exchange.EventExchange.Reader.ReadAsync();
        await Task.Delay(100);
        await pendingWrite;
    }

    [Fact]
    public async Task WriteAsync_WithBoundedChannelAndDropMode_ShouldNotWait()
    {
        // Arrange
        var options = Options.Create(new InMemoryExchangeOptions
        {
            ChannelOptions = new System.Threading.Channels.BoundedChannelOptions(2)
            {
                FullMode = System.Threading.Channels.BoundedChannelFullMode.DropNewest
            }
        });
        var exchange = new InMemoryEventExchange<TestEvent>(options);

        // Act
        await exchange.EventExchange.Writer.WriteAsync(new TestEvent { Value = 1 });
        await exchange.EventExchange.Writer.WriteAsync(new TestEvent { Value = 2 });
        await exchange.EventExchange.Writer.WriteAsync(new TestEvent { Value = 3 });

        exchange.EventExchange.Writer.Complete();

        // Assert
        var receivedEvents = new List<TestEvent>();
        await foreach (var evt in exchange.EventExchange.Reader.ReadAllAsync())
        {
            receivedEvents.Add(evt);
        }

        // Third event should be dropped
        receivedEvents.Count.ShouldBeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task WriteAsync_WithUnboundedChannel_ShouldNeverBeFull()
    {
        // Arrange
        var options = Options.Create(new InMemoryExchangeOptions
        {
            ChannelOptions = new System.Threading.Channels.UnboundedChannelOptions()
        });
        var exchange = new InMemoryEventExchange<TestEvent>(options);

        // Act
        for (int i = 0; i < 1000; i++)
        {
            await exchange.EventExchange.Writer.WriteAsync(new TestEvent { Value = i });
        }

        exchange.EventExchange.Writer.Complete();

        // Assert
        var count = 0;
        await foreach (var evt in exchange.EventExchange.Reader.ReadAllAsync())
        {
            count++;
        }

        count.ShouldBe(1000);
    }
}
