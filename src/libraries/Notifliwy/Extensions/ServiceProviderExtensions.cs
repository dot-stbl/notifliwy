using System;
using System.Collections.Generic;
using Notifliwy.Models.Interfaces;
using Notifliwy.Conditions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Handlers.Interfaces;
using Notifliwy.Steps.Interfaces;

namespace Notifliwy.Extensions;

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
    public static IEnumerable<INotificationStep<TNotification>> StepsBy<TNotification>(
        this IServiceProvider serviceProvider) 
            where TNotification : INotification
    {
        return serviceProvider.GetServices<INotificationStep<TNotification>>();
    }
    
    /// <summary>
    /// Return all assigned <see cref="INotificationStep{TNotification}"/>
    /// </summary>
    public static IEnumerable<IVerifierExecution> VerifiersBy(this IServiceProvider serviceProvider)
    {
        return serviceProvider.GetServices<IVerifierExecution>();
    }
    
    /// <summary>
    /// Return all assigned <see cref="INotificationExecutor{TEvent}"/>
    /// </summary>
    public static IEnumerable<INotificationExecutor<TEvent>> ExecutorsBy<TEvent>(this IServiceProvider serviceProvider) 
        where TEvent : IEvent
    {
        return serviceProvider.GetServices<INotificationExecutor<TEvent>>();
    }
    
    /// <summary>
    /// Return all assigned <see cref="INotificationHandler{TEvent}"/>
    /// </summary>
    public static IEnumerable<INotificationHandler<TEvent>> HandlersBy<TEvent>(this IServiceProvider serviceProvider) 
        where TEvent : IEvent
    {
        return serviceProvider.GetServices<INotificationHandler<TEvent>>();
    }
}