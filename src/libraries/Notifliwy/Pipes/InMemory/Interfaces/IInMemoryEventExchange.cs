using System.Threading.Channels;
using Notifliwy.Pipes.Interfaces;

namespace Notifliwy.Pipes.InMemory.Interfaces;

/// <summary>
/// <c>In memory</c> exchanger for <see cref="InMemoryInputPipe{TEvent}"/> and <see cref="InMemoryExportPipe{TEvent}"/>
/// </summary>
/// <typeparam name="TEvent">handle event</typeparam>
public interface IInMemoryEventExchange<TEvent>
{
    /// <summary>
    /// Event exchange by <see cref="IInputPipe{TEvent}"/> and <see cref="IExportPipe{TEvent}"/>
    /// </summary>
    public Channel<TEvent> EventExchange { get; }
}