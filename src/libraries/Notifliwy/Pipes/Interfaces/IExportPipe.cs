using System.Threading;
using System.Threading.Tasks;

namespace Notifliwy.Pipes.Interfaces;

/// <summary>
/// Export event assigned service pipe
/// </summary>
/// <typeparam name="TEvent">exported event</typeparam>
public interface IExportPipe<in TEvent>
{
    /// <summary>
    /// Export <typeparamref name="TEvent"/> to assigned <see cref="IExportPipe{TEvent}"/>
    /// </summary>
    /// <param name="exportEvent">exported event</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns></returns>
    public ValueTask ExportAsync(TEvent exportEvent, CancellationToken cancellationToken = default);
}