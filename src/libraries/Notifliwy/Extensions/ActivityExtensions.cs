using System;
using System.Diagnostics;
using Notifliwy.Connectors;
using Notifliwy.Handlers.Interfaces;

namespace Notifliwy.Extensions;

internal static class ActivityExtensions
{
    /// <summary>
    /// Invoke action <paramref name="metricAction"/> in pipeline creation activity
    /// </summary>
    public static Activity? AddMeter(this Activity? activity, Action metricAction)
    {
        metricAction.Invoke();
        return activity;
    }

    /// <summary>
    /// Create custom <see cref="Activity"/> special for <see cref="NotificationConnectorService{TEvent}"/>
    /// </summary>
    public static Activity? StartConnectorActivity(
        this ActivitySource activitySource,
        string name)
    {
        return activitySource.StartActivity(name, ActivityKind.Server);
    }
    
    /// <summary>
    /// Create custom <see cref="Activity"/> special for <see cref="INotificationHandler{TEvent}"/>
    /// </summary>
    public static Activity? StartHandlerActivity(
        this ActivitySource activitySource,
        string name)
    {
        return activitySource.StartActivity(name);
    }
}