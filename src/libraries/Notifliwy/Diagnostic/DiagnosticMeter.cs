using System.Diagnostics.Metrics;
using System.Reflection;

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
    public static Counter<long> EventCount { get; } = NotifliwyServerMeter.CreateCounter<long>(
        name: "notifliwy.server.event.count",
        description: "Number of events accepted");
    
    private static Meter CreateInstanceServerMeter(Assembly assembly)
    {
        return new Meter(
            name: $"{nameof(Notifliwy)}.Server", 
            version: $"{assembly.GetName().Version}");
    }
}