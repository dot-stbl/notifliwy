using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Notifliwy.Extensions;
using Notifliwy.Models.Interfaces;

namespace Notifliwy.Diagnostic.Additions;

internal class DiagnosticEventConstants
{
    public static readonly EventId ErrorSectorEvent = new (43_0002);
}

internal class DiagnosticConstants
{
    /// <summary>
    /// All traces prefix
    /// </summary>
    public static readonly string Prefix = nameof(Notifliwy).ToLower();
    
    public static class PropertyNames
    {
        /// <summary>
        /// Constant event type property
        /// </summary>
        public const string EventType = "event.type";

        /// <summary>
        /// Constant notification type property
        /// </summary>
        public const string NotificationType = "notification.type";
    }
}

// ReSharper disable once StaticMemberInGenericType
internal class DiagnosticEventData<TEvent>
    where TEvent : IEvent
{
    /// <summary>
    /// Current event constant name for diagnostic
    /// </summary>
    public static readonly string EventSeparation = typeof(TEvent).Name;

    /// <summary>
    /// Assigned Connector activity name
    /// </summary>
    public static readonly string ConnectorTraceName = $"{DiagnosticConstants.Prefix}.connector";
    
    /// <summary>
    /// Tag list by <typeparamref name="TEvent"/>
    /// </summary>
    public static readonly TagList TagsBy = new (
    [
        new KeyValuePair<string, object?>(DiagnosticConstants.PropertyNames.EventType, $"{typeof(TEvent)}".ToDotCase())
    ]);
}