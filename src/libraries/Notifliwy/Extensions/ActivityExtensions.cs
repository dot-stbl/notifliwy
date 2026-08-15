using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using Notifliwy.Connectors;
using Notifliwy.Contexts.Interfaces;
using Notifliwy.Diagnostic.Additions;

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
    /// Create custom <see cref="Activity"/> special for <see cref="NotificationConnector{TEvent}"/>
    /// </summary>
    public static Activity? StartConnectorActivity<TEvent>(this ActivitySource activitySource)
    {
        return activitySource
                .StartActivity(DiagnosticEventData<TEvent>.ConnectorTraceName, ActivityKind.Server)
                ?.AddTag(DiagnosticConstants.PropertyNames.EventType, DiagnosticEventData<TEvent>.EventSeparation);
    }

    /// <summary>
    /// Create custom <see cref="Activity"/> special for <see cref="INotificationSector{TEvent}"/>
    /// </summary>
    public static Activity? StartSectorActivity<TNotification, TEvent>(this ActivitySource activitySource)
    {
        return activitySource
                .StartActivity($"{DiagnosticSectorData<TNotification, TEvent>.TraceName}")
                .AddSectorTags<TNotification, TEvent>();
    }

    #region Tag extensions

    /// <summary>
    /// Add custom <c>notification</c> and <c>event</c> properties
    /// </summary>
    public static Activity? AddSectorTags<TNotification, TEvent>(this Activity? activity)
    {
        return activity
                ?.AddTag(
                    DiagnosticConstants.PropertyNames.EventType,
                    DiagnosticSectorData<TNotification, TEvent>.EventSeparation)
                .AddTag(
                    DiagnosticConstants.PropertyNames.NotificationType,
                    DiagnosticSectorData<TNotification, TEvent>.NotificationSeparation);
    }

    #endregion

    #region Exceptions

    /// <summary>
    /// Record Exception.
    /// </summary>
    /// <param name="activity">Activity instance.</param>
    /// <param name="exception">Exception to be recorded.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordException(this Activity? activity, Exception exception)
    {
        var tagsCollection = new ActivityTagsCollection
        {
            { SemanticConventions.AttributeExceptionType, exception.GetType().FullName },
            { SemanticConventions.AttributeExceptionStacktrace, exception.ToInvariantString() }
        };

        if (!string.IsNullOrWhiteSpace(exception.Message))
        {
            tagsCollection.Add(SemanticConventions.AttributeExceptionMessage, exception.Message);
        }

        activity?.AddEvent(
            new ActivityEvent(SemanticConventions.AttributeExceptionEventName,
                default,
                tagsCollection));
    }

    #endregion
}

file class SemanticConventions
{
    public const string AttributeExceptionEventName = "exception";
    public const string AttributeExceptionType = "exception.type";
    public const string AttributeExceptionMessage = "exception.message";
    public const string AttributeExceptionStacktrace = "exception.stacktrace";
}

/// <remarks>https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/Shared/ExceptionExtensions.cs</remarks>
file static class ExceptionExtensions
{
    /// <summary>
    /// Returns a culture-independent string representation of the given <paramref name="exception"/> object,
    /// appropriate for diagnostics tracing.
    /// </summary>
    /// <param name="exception">Exception to convert to string.</param>
    /// <returns>Exception as string with no culture.</returns>
    public static string ToInvariantString(this Exception exception)
    {
        var culture = Thread.CurrentThread.CurrentUICulture;

        try
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            return exception.ToString();
        }
        finally
        {
            Thread.CurrentThread.CurrentUICulture = culture;
        }
    }
}