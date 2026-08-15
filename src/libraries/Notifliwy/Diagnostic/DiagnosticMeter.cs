using System.Diagnostics.Metrics;
using System.Reflection;
using Notifliwy.Contexts.Interfaces;

namespace Notifliwy.Diagnostic;

internal static class DiagnosticMeter
{
    /// <summary>
    /// Global <see cref="Notifliwy"/> <see cref="Meter"/>
    /// </summary>
    public static readonly Meter NotifliwyServerMeter = CreateMeter(Assembly.GetExecutingAssembly());

    /// <summary>
    /// Event counter metric
    /// </summary>
    public static Counter<long> InputCounter { get; } = NotifliwyServerMeter.CreateCounter<long>(
        "notifliwy.server.event.count",
        description: "Number of events accepted");

    /// <summary>
    /// Event <see cref="INotificationSector{TEvent}"/> counter
    /// </summary>
    public static Counter<long> SectorProcessingCounter { get; } = NotifliwyServerMeter.CreateCounter<long>(
        "notifliwy.server.sector.count",
        description: "Number of events with final notification processing");

    private static Meter CreateMeter(Assembly assembly) =>
        new($"{nameof(Notifliwy)}.Server", $"{assembly.GetName().Version}");
}