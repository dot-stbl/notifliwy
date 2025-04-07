using System.Collections.Generic;
using System.Threading;
using Notifliwy.Models.Interfaces;

namespace Notifliwy.Pipes.Interfaces;

/// <summary>
/// Input event pipe
/// </summary>
/// <typeparam name="TEvent">current assigned event type</typeparam>
public interface IInputPipe<out TEvent>
    where TEvent : IEvent
{
    /// <summary>
    /// Get <typeparamref name="TEvent"/> imported from assigned <see cref="IExportPipe{TEvent}"/>
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns>current input event</returns>
    public IAsyncEnumerable<TEvent> AcceptAsync(CancellationToken cancellationToken = default);
}