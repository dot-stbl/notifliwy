using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Pipes.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notifliwy.Diagnostic;
using Notifliwy.Diagnostic.Additions;
using Notifliwy.Extensions;
using Notifliwy.Extensions.Dependency;

namespace Notifliwy.Connectors;

/// <summary>
/// <typeparamref name="TEvent"/> connector to all assigned <c>Notification</c>
/// </summary>
public class NotificationConnector<TEvent>(
    IInputPipe<TEvent> inputPipe,
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationConnector<TEvent>> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var sharedParallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        await using var connectorScope = scopeFactory.CreateAsyncScope()
                .SectorBy<TEvent>(out var sectors);

        logger.LogDebug("Assigned sectors: {SectorСount}", sectors.Length);

        while (!cancellationToken.IsCancellationRequested)
        {
            await foreach (var handledEvent in inputPipe.AcceptAsync(cancellationToken))
            {
                using var activity = DiagnosticActivity.NotifliwySource.StartConnectorActivity<TEvent>();

                await Parallel.ForEachAsync(
                    sectors,
                    sharedParallelOptions,
                    async (sector, token) =>
                    {
                        try
                        {
                            await sector.PassThroughAsync(handledEvent, token);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error in sector {SectorType} processing event {EventType}",
                                typeof(TEvent), sector.GetType().Name);
                            throw;
                        }
                    });

                activity.AddMeter(() =>
                {
                    DiagnosticMeter.InputCounter.Add(1, DiagnosticEventData<TEvent>.TagsBy);
                });
            }
        }
    }
}