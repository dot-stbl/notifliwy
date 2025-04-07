using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Models.Interfaces;
using Notifliwy.Pipes.InMemory.Interfaces;
using Notifliwy.Pipes.Interfaces;

namespace Notifliwy.Pipes.InMemory;

/// <summary>
/// <c>In memory</c> export pipe
/// </summary>
/// <param name="eventExchange">in memory exchange/queue</param>
/// <typeparam name="TEvent">current assigned event type</typeparam>
public class InMemoryExportPipe<TEvent>(IInMemoryEventExchange<TEvent> eventExchange) : IExportPipe<TEvent> 
    where TEvent : IEvent
{
    /// <inheritdoc cref="IExportPipe{TEvent}.ExportAsync"/>
    public async ValueTask ExportAsync(TEvent exportEvent, CancellationToken cancellationToken = default)
    {
        await eventExchange.EventExchange.Writer.WriteAsync(exportEvent, cancellationToken);
    }
}