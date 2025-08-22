using System.Diagnostics.Metrics;
using System.Reflection;
using Notifliwy.Contexts.Interfaces;

namespace Notifliwy.Diagnostic;

internal class DiagnosticMeter
{
    /// <summary>
    /// Global <see cref="Notifliwy"/> <see cref="Meter"/>
    /// </summary>
    public static readonly Meter NotifliwyServerMeter = CreateInstanceServerMeter(Assembly.GetExecutingAssembly());

    /// <summary>
    /// Event counter metric
    /// </summary>
    public static Counter<long> InputCounter { get; } = NotifliwyServerMeter.CreateCounter<long>(
        name: "notifliwy.server.event.count",
        description: "Number of events accepted");

    /// <summary>
    /// Event <see cref="INotificationSector{TEvent}"/> counter
    /// </summary>
    public static Counter<long> SectorProcessingCounter { get; } = NotifliwyServerMeter.CreateCounter<long>(
        name: "notifliwy.server.sector.count",
        description: "Number of events with final notification processing");

    private static Meter CreateInstanceServerMeter(Assembly assembly)
    {
        return new Meter(
            name: $"{nameof(Notifliwy)}.Server",
            version: $"{assembly.GetName().Version}");
    }
}