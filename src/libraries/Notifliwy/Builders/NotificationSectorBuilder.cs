using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Builders.Interfaces;
using Notifliwy.Builders.Internals;
using Notifliwy.Conditions;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Contexts.Compilers;
using Notifliwy.Contexts.Interfaces;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Extensions;
using Notifliwy.Handlers;
using Notifliwy.Handlers.Interfaces;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Models.Interfaces;
using Notifliwy.Options;
using Notifliwy.Steps.Interfaces;

namespace Notifliwy.Builders;

/// <summary>
/// Base <see cref="INotificationSectorBuilder"/>
/// </summary>
public class NotificationSectorBuilder<TNotification, TEvent>(IServiceCollection serviceCollection) : INotificationSectorBuilder
        where TNotification : INotification
        where TEvent : IEvent
{
    /// <summary>
    /// Assigned <see cref="INotificationConditionProcessor{TNotification,TEvent}"/> type
    /// </summary>
    protected Type ConditionProcessorOverride { get; private set; } = typeof(NotificationConditionProcessor<TNotification, TEvent>);

    /// <summary>
    /// Override default <see cref="INotificationConditionProcessor{TNotification,TEvent}"/> to <typeparamref name="TConditionProcessor"/>
    /// </summary>
    /// <typeparam name="TConditionProcessor">custom condition processor</typeparam>
    public NotificationSectorBuilder<TNotification, TEvent> OverrideConditionProcessor<TConditionProcessor>()
        where TConditionProcessor : class, INotificationConditionProcessor<TNotification, TEvent>
    {
        ConditionProcessorOverride = typeof(TConditionProcessor);
        serviceCollection.AddScoped<INotificationConditionProcessor<TNotification, TEvent>, TConditionProcessor>();
        return this;
    }

    /// <summary>
    /// Pending <see cref="INotificationCondition{TNotification,TEvent}"/> for addition to the final sector
    /// </summary>
    protected IList<Type> PendingConditions { get; } = [];
    
    /// <summary>
    /// Add to condition pipeline <typeparamref name="TCondition"/>
    /// </summary>
    /// <typeparam name="TCondition">custom condition handler</typeparam>
    public NotificationSectorBuilder<TNotification, TEvent> AddCondition<TCondition>()
        where TCondition : class, INotificationCondition<TNotification, TEvent>
    {
        PendingConditions.AddAction(
            source: typeof(TCondition), 
            actionAfter: _ =>
            {
                serviceCollection.AddScoped<INotificationCondition<TNotification, TEvent>, TCondition>();
            });
        
        return this;
    }
    
    /// <summary>
    /// Add to mapper event pipeline <typeparamref name="TMapper"/>
    /// </summary>
    /// <typeparam name="TMapper">custom mapper for <c>event</c> to <c>notification</c></typeparam>
    public NotificationSectorBuilder<TNotification, TEvent> AddMapper<TMapper>()
        where TMapper : class, INotificationMapper<TNotification, TEvent>
    {
        serviceCollection.AddScoped<INotificationMapper<TNotification, TEvent>, TMapper>();
        return this;
    }

    /// <summary>
    /// Add <see cref="INotificationExporter{TNotification}"/>
    /// </summary>
    public NotificationSectorBuilder<TNotification, TEvent> AddExporters<TExporter>()
        where TExporter : INotificationExporter<TNotification>
    {
        serviceCollection.AddScoped(
            serviceType: typeof(INotificationExporter<TNotification>),
            implementationType: typeof(TExporter));
        
        return this;
    }

    /// <summary>
    /// Current assigned pipeline builder
    /// </summary>
    internal IList<PipelineBuilder<TNotification>> StagesBuilders { get; } = [];
    
    /// <summary>
    /// Configure pipeline with <see cref="INotificationStep{TNotification}"/>
    /// </summary>
    public NotificationSectorBuilder<TNotification, TEvent> WithPipeline(
        Action<PipelineBuilder<TNotification>> pipelineBuilder)
    {
        StagesBuilders.AddAction(
            source: new PipelineBuilder<TNotification>(),
            actionAfter: pipelineBuilder.Invoke);
        
        return this;
    }
    
    /// <inheritdoc />
    public void RegisterSector()
    {
        serviceCollection.AddScoped(
            implementationType: typeof(NotificationExecutor<TEvent>),
            serviceType: typeof(INotificationExecutor<TEvent>));
        
        serviceCollection.AddScoped(
            implementationType: typeof(NotificationHandler<TNotification, TEvent>),
            serviceType: typeof(INotificationHandler<TEvent>));
        
        serviceCollection.AddScoped(
            implementationType: ConditionProcessorOverride,
            serviceType: typeof(INotificationConditionProcessor<TNotification, TEvent>));

        var conditions = PendingConditions.ToArray();
        {
            foreach (var condition in conditions)
            {
                serviceCollection.AddScoped(
                    implementationType: condition,
                    serviceType: typeof(INotificationCondition<TNotification, TEvent>));
            }
        
            var conditionOptions = new NotificationConditionOptions<TNotification, TEvent>
            {
                UseConditions = conditions.Length != 0,
                UseSingleCondition = conditions.Length == 1,
                UseMultiplyConditions = conditions.Length > 1
            };
        
            serviceCollection.AddSingleton(conditionOptions);
        }
        
        foreach (var pipelineBuilder in StagesBuilders.ToArray())
        {
            pipelineBuilder.BuildPipeline(serviceCollection);
        }
        
        serviceCollection.AddScoped<
            INotificationSector<TNotification, TEvent>, 
            NotificationSector<TNotification, TEvent>>();
        
        serviceCollection.AddScoped<SectorRuntimeCompiler<TNotification, TEvent>>();
    }
}