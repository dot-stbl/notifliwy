using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Builders.Interfaces;
using Notifliwy.Builders.Internals;
using Notifliwy.Conditions;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Contexts;
using Notifliwy.Contexts.Interfaces;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Extensions;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Steps.Interfaces;

namespace Notifliwy.Builders;

/// <summary>
/// Base <see cref="INotificationSectorBuilder"/>
/// </summary>
public class NotificationSectorBuilder<TNotification, TEvent>(IServiceCollection serviceCollection) : INotificationSectorBuilder
{
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
        PendingConditions.Add(item: typeof(TCondition));
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
    public NotificationSectorBuilder<TNotification, TEvent> AddExporter<TExporter>()
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
            implementationType: typeof(NotificationConditionProcessor<TNotification, TEvent>),
            serviceType: typeof(INotificationConditionProcessor<TNotification, TEvent>));

        var conditions = PendingConditions.ToArray();
        {
            foreach (var condition in conditions)
            {
                serviceCollection.AddScoped(
                    implementationType: condition,
                    serviceType: typeof(INotificationCondition<TNotification, TEvent>));
            }
        }

        foreach (var pipelineBuilder in StagesBuilders.ToArray())
        {
            pipelineBuilder.BuildPipeline(serviceCollection);
        }

        serviceCollection.AddScoped(typeof(SectorBlock<TNotification, TEvent>));

        //as full generic
        serviceCollection.AddTransient(
            serviceType: typeof(INotificationSector<TEvent>),
            implementationType: typeof(NotificationSector<TNotification, TEvent>));
    }
}