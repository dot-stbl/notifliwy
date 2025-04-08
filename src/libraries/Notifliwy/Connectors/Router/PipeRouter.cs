using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Connectors.Router.Interfaces;
using Notifliwy.Diagnostic;
using Notifliwy.Diagnostic.Additions;
using Notifliwy.Extensions;
using Notifliwy.Handlers.Interfaces;
using Notifliwy.Models.Interfaces;
using Notifliwy.Pipes.Interfaces;

namespace Notifliwy.Connectors.Router;

/// <inheritdoc />
public class PipeRouter<TEvent>(IInputPipe<TEvent> inputPipe, INotificationExecutor<TEvent>[] executors) : IPipeRouter
    where TEvent : IEvent
{
    /// <summary>Factory create ready <see cref="IPipeRouter"/></summary>
    public static PipeRouter<TEvent>[] CreateInstance(
        int workerCount,
        IInputPipe<TEvent> inputPipe, 
        INotificationExecutor<TEvent>[] executors)
    {
        return Enumerable.Range(0, workerCount)
            .Select(_ => new PipeRouter<TEvent>(inputPipe, executors))
            .ToArray();
    }

    /// <inheritdoc />
    public async Task RunAsync(ParallelOptions parallelOptions, CancellationToken cancellationToken)
    {
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
        }
    }
}