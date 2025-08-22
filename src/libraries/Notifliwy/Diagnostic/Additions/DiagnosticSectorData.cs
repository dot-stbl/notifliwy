using System.Collections.Generic;
using System.Diagnostics;

namespace Notifliwy.Diagnostic.Additions;

// ReSharper disable once StaticMemberInGenericType
internal class DiagnosticSectorData<TNotification, TEvent>
{
    /// <summary>
    /// Current notification constant name for diagnostic
    /// </summary>
    public static readonly string NotificationSeparation = typeof(TNotification).Name;
    
    /// <summary>
    /// Current event constant name for diagnostic
    /// </summary>
    public static readonly string EventSeparation = DiagnosticEventData<TEvent>.EventSeparation;

    /// <summary>
    /// Tag list by <typeparamref name="TEvent"/> with <typeparamref name="TNotification"/>
    /// </summary>
    public static readonly TagList TagsBy = new (
    [
        new KeyValuePair<string, object?>("event.type", $"{typeof(TEvent)}"),
        new KeyValuePair<string, object?>("notification.type", $"{typeof(TNotification)}")
    ]);

    /// <summary>
    /// Activity name by sector
    /// </summary>
    public static readonly string TraceName = $"{DiagnosticConstants.Prefix}.transaction.sector";
}