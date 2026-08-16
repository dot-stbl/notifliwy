using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Contexts.Interfaces;
using Notifliwy.Diagnostic.Additions;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Extensions.Dependency;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Related;
using Notifliwy.Steps.Interfaces;

namespace Notifliwy.Contexts;

/// <summary>
/// Stores and retrieves all services required for <see cref="INotificationSector{TEvent}"/>,
///     for further compilation
/// </summary>
/// <inheritdoc cref="ISectorBlock"/>
/// <typeparam name="TNotification">assigned notification type</typeparam>
/// <typeparam name="TEvent">bound event type</typeparam>
public class SectorBlock<TNotification, TEvent>(
    IServiceProvider serviceProvider,
    ILogger<SectorBlock<TNotification, TEvent>> sectorLogger,
    INotificationConditionProcessor<TNotification, TEvent> conditionProcessor) : ISectorBlock
{
    /// <summary>
    /// Bound <see cref="INotificationExporter{TNotification}"/> instances
    /// </summary>
    public MultiplyServiceInstance<INotificationExporter<TNotification>> ExporterInstances { get; }
        = serviceProvider.ExporterBy<TNotification>().ToMultiplyService();

    /// <summary>
    /// Bound <see cref="INotificationPipeline{TNotification}"/> instances
    /// </summary>
    public MultiplyServiceInstance<INotificationPipeline<TNotification>> PipelineInstances { get; }
        = serviceProvider.PipelinesBy<TNotification>().ToMultiplyService();

    /// <summary>
    /// Bound <see cref="INotificationCondition{TNotification,TEvent}"/> instances
    /// </summary>
    public MultiplyServiceInstance<INotificationCondition<TNotification, TEvent>> ConditionInstances { get; }
        = serviceProvider.ConditionsBy<TNotification, TEvent>().ToMultiplyService();

    /// <summary>
    /// Bound <see cref="INotificationMapper{TNotification,TEvent}"/> instance
    /// </summary>
    public INotificationMapper<TNotification, TEvent> MapperSector { get; }
        = serviceProvider.MapperBy<TNotification, TEvent>();

    /// <summary>
    /// The main method that performs all the basic logic for processing events and carrying notification to endpoints
    /// </summary>
    public async ValueTask ProcessingAsync(
        TEvent inputEvent,
        CancellationToken cancellationToken = default)
    {
        if (ConditionInstances.UseInstance
            && !await ConditionInstances.CheckoutInstanceAsync(
                async condition
                        => await conditionProcessor.AllowConditionAsync(inputEvent, condition, cancellationToken),
                async conditions
                        => await conditionProcessor.AllowConditionsAsync(inputEvent, conditions, cancellationToken)))
        {
            return;
        }

        sectorLogger.LogDebug("InputEventHash: {InputEventHash} / InputEvent: {InputEvent} is allow, continue processing",
            inputEvent?.GetHashCode(),
            DiagnosticEventData<TEvent>.EventSeparation);

        var aggregatedNotification = await MapperSector.ConvertAsync(inputEvent, cancellationToken);

        if (PipelineInstances.UseInstance)
        {
            aggregatedNotification = await PipelineInstances.CheckoutInstanceAsync(
                async pipeline => await pipeline.InvokePipeline(aggregatedNotification, cancellationToken),
                async pipelines =>
                {
                    foreach (var pipeline in pipelines)
                    {
                        aggregatedNotification = await pipeline.InvokePipeline(aggregatedNotification, cancellationToken);
                    }

                    return aggregatedNotification;
                });
        }

        if (ExporterInstances.UseInstance)
        {
            await ExporterInstances.CheckoutInstanceAsync(
                exporter => exporter.ThrowAsync(aggregatedNotification, cancellationToken),
                async exporters =>
                {
                    foreach (var exporter in exporters)
                    {
                        await exporter.ThrowAsync(aggregatedNotification, cancellationToken);
                    }
                });
        }
    }
}