using System.Collections.Generic;
using System.Diagnostics;
using Notifliwy.Extensions;
using Notifliwy.Models.Interfaces;

namespace Notifliwy.Diagnostic.Additions;

internal class DiagnosticConstants
{
    /// <summary>
    /// All traces prefix
    /// </summary>
    public static readonly string Prefix = nameof(Notifliwy).ToLower();
}

// ReSharper disable once StaticMemberInGenericType
internal class DiagnosticEventData<TEvent>
    where TEvent : IEvent
{
    /// <summary>
    /// Current event constant name for diagnostic
    /// </summary>
    public static readonly string EventSeparation = typeof(TEvent).Name.ToDotCase();

    /// <summary>
    /// Assigned Connector activity name
    /// </summary>
    public static readonly string ConnectorTraceName = $"{DiagnosticConstants.Prefix}.connector-{EventSeparation}";
    
    /// <summary>
    /// Tag list by <typeparamref name="TEvent"/>
    /// </summary>
    public static readonly TagList TagsBy = new ([new KeyValuePair<string, object?>("event.type", $"{typeof(TEvent)}".ToDotCase())]);
}

// ReSharper disable once StaticMemberInGenericType
internal class DiagnosticSectorData<TNotification, TEvent> 
    where TNotification : INotification
    where TEvent : IEvent
{
    /// <summary>
    /// Current event constant name for diagnostic
    /// </summary>
    public static readonly string NotificationSeparation = typeof(TNotification).Name.ToDotCase();
}