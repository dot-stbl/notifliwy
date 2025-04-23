using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Models.Interfaces;
using Notifliwy.Steps.Interfaces;

namespace Notifliwy.Extensions.Dependency;

internal static class ServiceProviderExtensions
{
    /// <summary>
    /// Find collection of <see cref="INotificationCondition{TNotification,TEvent}"/>
    /// </summary>
    /// <returns><see cref="INotificationCondition{TNotification,TEvent}"/> collection</returns>
    public static IEnumerable<INotificationCondition<TNotification, TEvent>> ConditionsBy<TNotification, TEvent>(
        this IServiceProvider serviceProvider)
        where TNotification : INotification
        where TEvent : IEvent
    {
        return serviceProvider.GetServices<INotificationCondition<TNotification, TEvent>>();
    }

    /// <summary>
    /// Return all assigned <see cref="INotificationStep{TNotification}"/>
    /// </summary>
    public static IEnumerable<INotificationPipeline<TNotification>> PipelinesBy<TNotification>(
        this IServiceProvider serviceProvider) 
            where TNotification : INotification
    {
        return serviceProvider.GetServices<INotificationPipeline<TNotification>>();
    }
    
    /// <summary>
    /// Create <see cref="INotificationExporter{TNotification}"/> for new scope
    /// </summary>
    public static IEnumerable<INotificationExporter<TNotification>> ExporterBy<TNotification>(
        this IServiceProvider serviceProvider) 
            where TNotification : INotification
    {
        return serviceProvider.GetServices<INotificationExporter<TNotification>>();
    }
    
    /// <summary>
    /// Get from <paramref name="serviceProvider"/> services as <see cref="INotificationMapper{TNotification,TEvent}"/>
    /// </summary>
    public static INotificationMapper<TNotification, TEvent> MapperBy<TNotification, TEvent>(
        this IServiceProvider serviceProvider)
        where TNotification : INotification 
        where TEvent : IEvent
    {
        return serviceProvider.GetRequiredService<INotificationMapper<TNotification, TEvent>>();
    }
}