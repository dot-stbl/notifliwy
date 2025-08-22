using System.Collections.Generic;
using System.Threading;

namespace Notifliwy.Pipes.Interfaces;

/// <summary>
/// Input event pipe
/// </summary>
/// <typeparam name="TEvent">current assigned event type</typeparam>
public interface IInputPipe<out TEvent>
{
    /// <summary>
    /// Get <typeparamref name="TEvent"/> imported from assigned <see cref="IExportPipe{TEvent}"/>
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns>current input event</returns>
    public IAsyncEnumerable<TEvent> AcceptAsync(CancellationToken cancellationToken = default);
}