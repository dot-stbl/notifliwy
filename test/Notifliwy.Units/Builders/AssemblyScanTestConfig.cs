using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Config.Interfaces;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Graph.Interfaces;

namespace Notifliwy.Units.Builders;

/// <summary>
/// Event consumed by the assembly-scanned sector.
/// </summary>
public sealed class ScanEvent
{
    /// <summary>
    /// Simple payload value.
    /// </summary>
    public int Value { get; init; }
}

/// <summary>
/// Notification produced by the assembly-scanned sector.
/// </summary>
public sealed class ScanNotification
{
    /// <summary>
    /// Mapped payload value.
    /// </summary>
    public int Value { get; set; }
}

/// <summary>
/// Static sink for the assembly-scan test exports.
/// </summary>
public static class AssemblyScanSinks
{
    /// <summary>
    /// Captured exports of the assembly-scanned sector.
    /// </summary>
    public static ConcurrentQueue<ScanNotification> Exports { get; } = new();
}

/// <summary>
/// Public top-level sector config discovered by <c>AddSectorsFromAssembly</c>. Must
/// stay public and top-level (the reflection fallback sees only visible types) and
/// must remain the only public config in this assembly so scan tests stay deterministic.
/// </summary>
public sealed class AssemblyScanConfig : INotificationSectorConfig<ScanNotification, ScanEvent>
{
    /// <inheritdoc />
    public void Configure(ISectorGraphBuilder<ScanNotification, ScanEvent> graph)
    {
        graph
            .Map((inputEvent, cancellationToken) =>
                ValueTask.FromResult(new ScanNotification { Value = inputEvent.Value * 3 }))
            .Export<ScanSinkExporter>();
    }
}

/// <summary>
/// Parameterless exporter writing into the static sink, so the scanned sector is
/// compile-safe under Auto execution.
/// </summary>
public sealed class ScanSinkExporter : INotificationExporter<ScanNotification>
{
    /// <inheritdoc />
    public ValueTask ThrowAsync(ScanNotification notification, CancellationToken cancellationToken = default)
    {
        AssemblyScanSinks.Exports.Enqueue(notification);
        return ValueTask.CompletedTask;
    }
}
