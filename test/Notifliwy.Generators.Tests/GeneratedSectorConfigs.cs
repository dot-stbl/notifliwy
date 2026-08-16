using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Config;
using Notifliwy.Config.Interfaces;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Graph.Interfaces;
using Notifliwy.Mapper.Interfaces;

// marks THIS test assembly for sector source generation: the generator under test
// emits NotifliwySectorsRegistration.AddNotifliwySectors() from the configs below
[assembly: NotifliwySectors]

namespace Notifliwy.Generators.Tests;

/// <summary>
/// Event consumed by the alpha sector.
/// </summary>
public sealed class AlphaEvent
{
    /// <summary>
    /// Simple payload value.
    /// </summary>
    public int Value { get; init; }
}

/// <summary>
/// Notification produced by the alpha sector.
/// </summary>
public sealed class AlphaNotification
{
    /// <summary>
    /// Mapped payload value.
    /// </summary>
    public int Value { get; set; }
}

/// <summary>
/// Event consumed by the beta sector.
/// </summary>
public sealed class BetaEvent
{
    /// <summary>
    /// Simple payload text.
    /// </summary>
    public string Text { get; init; } = string.Empty;
}

/// <summary>
/// Notification produced by the beta sector.
/// </summary>
public sealed class BetaNotification
{
    /// <summary>
    /// Mapped payload text.
    /// </summary>
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Static export sinks shared with the parameterless compiled-path nodes.
/// </summary>
public static class GeneratedSectorSinks
{
    /// <summary>
    /// Alpha sector exports.
    /// </summary>
    public static ConcurrentQueue<AlphaNotification> AlphaExports { get; } = new();

    /// <summary>
    /// Beta sector exports.
    /// </summary>
    public static ConcurrentQueue<BetaNotification> BetaExports { get; } = new();
}

/// <summary>
/// Alpha sector config: class mapper + condition + exporter, all stateless.
/// </summary>
public sealed class AlphaSectorConfig : INotificationSectorConfig<AlphaNotification, AlphaEvent>
{
    /// <inheritdoc />
    public void Configure(ISectorGraphBuilder<AlphaNotification, AlphaEvent> graph)
    {
        graph
            .When<PositiveValueCondition>()
            .Map<AlphaMapper>()
            .Export<AlphaExporter>();
    }
}

/// <summary>
/// Beta sector config: inline lambda map + exporter.
/// </summary>
public sealed class BetaSectorConfig : INotificationSectorConfig<BetaNotification, BetaEvent>
{
    /// <inheritdoc />
    public void Configure(ISectorGraphBuilder<BetaNotification, BetaEvent> graph)
    {
        graph
            .Map((inputEvent, cancellationToken) =>
                ValueTask.FromResult(new BetaNotification { Text = inputEvent.Text.ToUpperInvariant() }))
            .Export<BetaExporter>();
    }
}

/// <summary>
/// Filters out non-positive alpha events.
/// </summary>
public sealed class PositiveValueCondition : INotificationCondition<AlphaNotification, AlphaEvent>
{
    /// <inheritdoc />
    public ValueTask<bool> AllowItAsync(AlphaEvent inputEvent, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(inputEvent.Value > 0);
    }
}

/// <summary>
/// Doubles the alpha event value.
/// </summary>
public sealed class AlphaMapper : INotificationMapper<AlphaNotification, AlphaEvent>
{
    /// <inheritdoc />
    public ValueTask<AlphaNotification> ConvertAsync(
        AlphaEvent inputEvent,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(new AlphaNotification { Value = inputEvent.Value * 2 });
    }
}

/// <summary>
/// Captures alpha exports into the static sink.
/// </summary>
public sealed class AlphaExporter : INotificationExporter<AlphaNotification>
{
    /// <inheritdoc />
    public ValueTask ThrowAsync(AlphaNotification notification, CancellationToken cancellationToken = default)
    {
        GeneratedSectorSinks.AlphaExports.Enqueue(notification);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Captures beta exports into the static sink.
/// </summary>
public sealed class BetaExporter : INotificationExporter<BetaNotification>
{
    /// <inheritdoc />
    public ValueTask ThrowAsync(BetaNotification notification, CancellationToken cancellationToken = default)
    {
        GeneratedSectorSinks.BetaExports.Enqueue(notification);
        return ValueTask.CompletedTask;
    }
}
