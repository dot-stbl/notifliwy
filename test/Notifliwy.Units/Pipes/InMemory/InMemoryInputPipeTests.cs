using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Notifliwy.Pipes.InMemory;
using Notifliwy.Pipes.InMemory.Interfaces;
using Notifliwy.Pipes.InMemory.Options;
using Shouldly;
using Xunit;

namespace Notifliwy.Units.Pipes.InMemory;

/// <summary>
/// Unit tests for <see cref="InMemoryInputPipe{TEvent}"/>
/// </summary>
public class InMemoryInputPipeTests
{
    private class TestEvent
    {
        public int Value { get; init; }
    }

    [Fact]
    public async Task AcceptAsync_ShouldReadFromExchange()
    {
        // Arrange
        var exchange = new InMemoryEventExchange<TestEvent>(exchangeOptions: null);
        var pipe = new InMemoryInputPipe<TestEvent>(exchange);

        // Act
        await exchange.EventExchange.Writer.WriteAsync(new TestEvent { Value = 1 });
        await exchange.EventExchange.Writer.WriteAsync(new TestEvent { Value = 2 });
        await exchange.EventExchange.Writer.WriteAsync(new TestEvent { Value = 3 });
        exchange.EventExchange.Writer.Complete();

        var receivedEvents = new List<TestEvent>();
        await foreach (var evt in pipe.AcceptAsync())
        {
            receivedEvents.Add(evt);
        }

        // Assert
        receivedEvents.Count.ShouldBe(3);
        receivedEvents[0].Value.ShouldBe(1);
        receivedEvents[1].Value.ShouldBe(2);
        receivedEvents[2].Value.ShouldBe(3);
    }

    [Fact]
    public async Task AcceptAsync_ShouldReturnEmptyWhenExchangeIsEmpty()
    {
        // Arrange
        var exchange = new InMemoryEventExchange<TestEvent>(exchangeOptions: null);
        exchange.EventExchange.Writer.Complete();
        var pipe = new InMemoryInputPipe<TestEvent>(exchange);

        // Act
        var receivedEvents = new List<TestEvent>();
        await foreach (var evt in pipe.AcceptAsync())
        {
            receivedEvents.Add(evt);
        }

        // Assert
        receivedEvents.Count.ShouldBe(0);
    }
}
