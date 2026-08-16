using Notifliwy.Config.Interfaces;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Graph.Interfaces;
using Notifliwy.Join.Interfaces;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Transform.Interfaces;

namespace Notifliwy.Sample.InMemory;

/// <summary>
/// Incoming event produced by <see cref="PutterService"/> through the in-memory pipe.
/// </summary>
public record TelemetryEvent(int Value);

/// <summary>
/// Notification flowing through the sector graph. Immutable on purpose: a
/// <c>Branch</c> fan-out shares one notification instance between branches, so
/// transforms return new copies via <c>with</c> instead of mutating the input.
/// </summary>
public record TelemetryReport(int Value, string Channel = "raw");

/// <summary>
/// Sector described as a config class: When → Map → Branch(console | audit) →
/// Join → Export. Discovered by the source generator through
/// <c>[assembly: NotifliwySectors]</c> and registered by the generated
/// <c>AddNotifliwySectors()</c> call.
/// </summary>
public class TelemetrySector : INotificationSectorConfig<TelemetryReport, TelemetryEvent>
{
    /// <inheritdoc />
    public void Configure(ISectorGraphBuilder<TelemetryReport, TelemetryEvent> graph)
    {
        graph
            .When<MultipleOfFiveCondition>()
            .Map<TelemetryReportMapper>()
            .Branch(
                branch => branch
                    .Transform<DoubleForConsoleTransform>()
                    .Export<ConsoleExporter>(),
                branch => branch
                    .Transform<DoubleForAuditTransform>()
                    .Export<AuditExporter>())
            .Join<ChannelMergeJoin>()
            .Export<SummaryExporter>();
    }
}

/// <inheritdoc />
public class MultipleOfFiveCondition : INotificationCondition<TelemetryReport, TelemetryEvent>
{
    /// <inheritdoc />
    public ValueTask<bool> AllowItAsync(
        TelemetryEvent inputEvent,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(inputEvent.Value % 5 == 0);
    }
}

/// <inheritdoc />
public class TelemetryReportMapper : INotificationMapper<TelemetryReport, TelemetryEvent>
{
    /// <inheritdoc />
    public ValueTask<TelemetryReport> ConvertAsync(
        TelemetryEvent inputEvent,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(new TelemetryReport(inputEvent.Value));
    }
}

/// <inheritdoc />
public class DoubleForConsoleTransform : INotificationTransform<TelemetryReport>
{
    /// <inheritdoc />
    public ValueTask<TelemetryReport> TransformAsync(
        TelemetryReport notification,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(notification with { Value = notification.Value * 2, Channel = "console" });
    }
}

/// <inheritdoc />
public class DoubleForAuditTransform : INotificationTransform<TelemetryReport>
{
    /// <inheritdoc />
    public ValueTask<TelemetryReport> TransformAsync(
        TelemetryReport notification,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(notification with { Value = notification.Value * 3, Channel = "audit" });
    }
}

/// <inheritdoc />
public class ConsoleExporter : INotificationExporter<TelemetryReport>
{
    /// <inheritdoc />
    public ValueTask ThrowAsync(
        TelemetryReport notification,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[console] value={notification.Value}");

        return ValueTask.CompletedTask;
    }
}

/// <inheritdoc />
public class AuditExporter : INotificationExporter<TelemetryReport>
{
    /// <inheritdoc />
    public ValueTask ThrowAsync(
        TelemetryReport notification,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[audit]   value={notification.Value}");

        return ValueTask.CompletedTask;
    }
}

/// <inheritdoc />
public class ChannelMergeJoin : INotificationJoin<TelemetryReport>
{
    /// <inheritdoc />
    public ValueTask<TelemetryReport> JoinAsync(
        IReadOnlyList<TelemetryReport> notifications,
        CancellationToken cancellationToken = default)
    {
        var value = notifications.Max(report => report.Value);
        var channel = string.Join("+", notifications.Select(report => report.Channel));

        return ValueTask.FromResult(new TelemetryReport(value, channel));
    }
}

/// <inheritdoc />
public class SummaryExporter : INotificationExporter<TelemetryReport>
{
    /// <inheritdoc />
    public ValueTask ThrowAsync(
        TelemetryReport notification,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[summary] joined channels={notification.Channel} value={notification.Value}");
        Console.WriteLine();

        return ValueTask.CompletedTask;
    }
}
