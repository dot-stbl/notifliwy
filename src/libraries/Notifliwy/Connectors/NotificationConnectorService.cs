using System.Linq;
using System.Threading;
using Notifliwy.Diagnostic;
using System.Threading.Tasks;
using Notifliwy.Pipes.Interfaces;
using Notifliwy.Models.Interfaces;
using Microsoft.Extensions.Hosting;
using Notifliwy.Diagnostic.Additions;
using Notifliwy.Handlers.Interfaces;
using Notifliwy.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Notifliwy.Connectors;

/// <summary>
/// <typeparamref name="TEvent"/> connector to all assigned <see cref="INotification"/>
/// </summary>
public class NotificationConnectorService<TEvent>(
    IInputPipe<TEvent> inputPipe,
    IServiceScopeFactory scopeFactory) : BackgroundService
        where TEvent : IEvent
{
    private const int WorkerCount = 8;
    
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await using var connectorScope = scopeFactory.CreateAsyncScope();
        
        var executors = connectorScope.ServiceProvider
            .ExecutorsBy<TEvent>()
            .ToArray();
        
        var tasks = Enumerable.Range(0, WorkerCount)
            .Select(_ => Task.Run(function: async () 
                => await RunPipeRouter(
                    executors, 
                    cancellationToken), 
                cancellationToken))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    internal async Task RunPipeRouter(
        INotificationExecutor<TEvent>[] executors, 
        CancellationToken cancellationToken)
    {
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken
        };
        
        while (!cancellationToken.IsCancellationRequested)
        {
            await foreach (var inputEvent in inputPipe.AcceptAsync(cancellationToken))
            {
                using var activity = DiagnosticActivity.NotifliwySource
                    .StartConnectorActivity(name: DiagnosticEventData<TEvent>.ConnectorTraceName)
                    .AddMeter(metricAction: () =>
                    {
                        DiagnosticMeter.EventCount.Add(delta: 1, tagList: DiagnosticEventData<TEvent>.TagsBy);
                    });
                
                await Parallel.ForEachAsync(
                    executors, 
                    parallelOptions, 
                    body: async (executor, ct) =>
                    {
                        await executor.StartAsync(inputEvent, ct);
                    });
            }
            
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}