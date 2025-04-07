using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Contexts.Interfaces;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Extensions;
using Notifliwy.Handlers.Interfaces;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Models.Interfaces;
using Notifliwy.Options;

namespace Notifliwy.Contexts.Compilers;

/// <summary>
/// File extensions builder for <see cref="INotificationSector{TNotification,TEvent}"/>
/// </summary>
public class SectorRuntimeCompiler<TNotification, TEvent>(
    IServiceProvider serviceProvider,
    ILogger<SectorRuntimeCompiler<TNotification, TEvent>> logger,
    NotificationConditionOptions<TNotification, TEvent> conditionOptions) 
        where TNotification : INotification 
        where TEvent : IEvent
{
    /// <summary>
    /// Compile <see cref="INotificationSector{TNotification,TEvent}.CompiledCondition"/>
    /// </summary>
    public Func<TEvent, CancellationToken, ValueTask<bool>> CompileCondition()
    {
        var conditionProcessor = serviceProvider.GetService
            <INotificationConditionProcessor<TNotification, TEvent>?>();
        
        if (conditionOptions.UseConditions && conditionProcessor != null)
        {
            var notificationConditions = serviceProvider
                .ConditionsBy<TNotification, TEvent>()
                .ToArray();
            
            if (conditionOptions.UseSingleCondition && 
                notificationConditions.Length == 1 && 
                notificationConditions.FirstOrDefault() is {} condition)
            {
                return async (inputEvent, cancellationToken) => await conditionProcessor.AllowConditionAsync(
                    inputEvent,
                    condition,
                    cancellationToken);
            }

            if (conditionOptions.UseMultiplyConditions && notificationConditions.Length > 1)
            {
                return async (inputEvent, cancellationToken) => await conditionProcessor.AllowConditionsAsync(
                    inputEvent,
                    notificationConditions,
                    cancellationToken);
            }
        }

        return (_, _) => ValueTask.FromResult(true);
    }
    
    /// <summary>
    /// Compile <see cref="INotificationSector{TNotification,TEvent}.CompiledExporter"/>
    /// </summary>
    public Func<TNotification, CancellationToken, ValueTask> CompileExporters()
    {
        var exporters = serviceProvider
            .GetServices<INotificationExporter<TNotification>>()
            .ToArray();

        if (exporters.Length == 1 && exporters.FirstOrDefault() is {} singleExporter)
        {
            return (notification, cancellationToken) =>
            {
                 _ = Task.Run(
                    function: async () => await singleExporter.ThrowAsync(notification, cancellationToken), 
                    cancellationToken);

                 return ValueTask.CompletedTask;
            };
        }

        if(exporters.Length > 1)
        {
            return (notification, cancellationToken) =>
            {
                foreach (var exporter in exporters)
                {
                    _ = Task.Run(
                        function: async () => await exporter.ThrowAsync(notification, cancellationToken), 
                        cancellationToken);
                }
                
                return ValueTask.CompletedTask;
            };
        }
        
        logger.LogWarning(message: "No exporters assigned for this notification type");
            
        return (notification, _) =>
        {
            logger.LogInformation(
                message: "Export notification: {Notification}", 
                args: JsonSerializer.Serialize(notification));
            
            return ValueTask.CompletedTask;
        };
    }
    
    /// <summary>
    /// Compile <see cref="INotificationSector{TNotification,TEvent}.CompiledExporter"/>
    /// </summary>
    public Func<TEvent, CancellationToken, ValueTask<TNotification>> CompileMapper()
    {
        var mapper = serviceProvider.GetRequiredService<INotificationMapper<TNotification, TEvent>>();
        return async (inputEvent, token) => await mapper.ConvertAsync(inputEvent, token);
    }
    
    /// <summary>
    /// Compile <see cref="INotificationSector{TNotification,TEvent}.CompiledExporter"/>
    /// </summary>
    public Func<TNotification, CancellationToken, ValueTask<TNotification>> CompilePipeline()
    {
        var pipelines = serviceProvider
            .StepsBy<TNotification>()
            .ToArray();
        
        if (pipelines.Length != 0)
        {
            return async (notification, cancellationToken) =>
            {
                return await pipelines.AggregateAsync(
                    notification,
                    func: async (aggregateNotification, step) 
                        => await step.AggregateAsync(aggregateNotification, cancellationToken));
            };
        }

        return (notification, _) => ValueTask.FromResult(notification);
    }
}

internal class HandlerVerifierOptions<TNotification, TEvent>
    where TNotification : INotification
    where TEvent : IEvent
{
    /// <summary>
    /// Collection assigned <see cref="IVerifierExecution"/>
    /// </summary>
    public IReadOnlyCollection<Type> VerifiersExecution { get; } = [];
}