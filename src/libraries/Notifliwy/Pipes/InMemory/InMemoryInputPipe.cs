using System.Threading;
using Notifliwy.Pipes.Interfaces;
using System.Collections.Generic;
using Notifliwy.Models.Interfaces;
using System.Runtime.CompilerServices;
using Notifliwy.Pipes.InMemory.Interfaces;

namespace Notifliwy.Pipes.InMemory;

/// <summary>
/// <c>In memory</c> input pipe
/// </summary>
/// <param name="eventExchange">in memory exchange/queue</param>
/// <typeparam name="TEvent">current assigned event type</typeparam>
public class InMemoryInputPipe<TEvent>(IInMemoryEventExchange<TEvent> eventExchange) : IInputPipe<TEvent> 
    where TEvent : IEvent
{
    /// <inheritdoc cref="IInputPipe{TEvent}.AcceptAsync"/>
    public async IAsyncEnumerable<TEvent> AcceptAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var inputEvent in eventExchange.EventExchange.Reader.ReadAllAsync(cancellationToken))
        {
            yield return inputEvent;
        }
    }
}