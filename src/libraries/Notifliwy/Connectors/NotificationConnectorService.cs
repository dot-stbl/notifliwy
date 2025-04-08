using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Pipes.Interfaces;
using Notifliwy.Models.Interfaces;
using Microsoft.Extensions.Hosting;
using Notifliwy.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Connectors.Router;
using Notifliwy.Options;

namespace Notifliwy.Connectors;

/// <summary>
/// <typeparamref name="TEvent"/> connector to all assigned <see cref="INotification"/>
/// </summary>
public class NotificationConnectorService<TEvent>(
    IInputPipe<TEvent> inputPipe,
    IServiceScopeFactory scopeFactory,
    NotificationConnectorOptions<TEvent> connectorOptions) : BackgroundService
        where TEvent : IEvent
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await using var connectorScope = scopeFactory.CreateAsyncScope();
        
        var executors = connectorScope.ServiceProvider
            .ExecutorsBy<TEvent>()
            .ToArray();

        var sharedParallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken
        };

        var routerExecute = PipeRouter<TEvent>.CreateInstance(
                inputPipe: inputPipe, 
                executors: executors,
                workerCount: connectorOptions.WorkerCount)
            .Select((router, _) => router.RunAsync(sharedParallelOptions, cancellationToken));
        
        await Task.WhenAll(routerExecute);
    }
}