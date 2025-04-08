using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Handlers.Interfaces;

namespace Notifliwy.Connectors.Router.Interfaces;

/// <summary>
/// Internal input pipe router
/// </summary>
public interface IPipeRouter
{
    /// <summary>
    /// Start parallel processing by <paramref name="parallelOptions"/> and <see cref="INotificationExecutor{TEvent}"/>
    /// </summary>
    /// <param name="parallelOptions"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task RunAsync(ParallelOptions parallelOptions, CancellationToken cancellationToken);
}