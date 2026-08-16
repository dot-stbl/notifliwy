using Notifliwy.Graph;
using Notifliwy.Graph.Interfaces;

namespace Notifliwy.Config.Interfaces;

/// <summary>
/// Defines a sector as a configuration class: a typed graph of nodes plus
/// sector-level execution options. Register with
/// <see cref="Builders.NotificationServerBuilder.AddSector{TConfig}"/>.
/// </summary>
/// <typeparam name="TNotification">The notification type produced by the graph <c>Map</c> node</typeparam>
/// <typeparam name="TEvent">The event type consumed by the sector</typeparam>
/// <example>
/// <code>
/// public class PaymentSector : INotificationSectorConfig&lt;PaymentNotification, PaymentEvent&gt;
/// {
///     public void Configure(ISectorGraphBuilder&lt;PaymentNotification, PaymentEvent&gt; graph)
///     {
///         graph
///             .When&lt;LargePaymentCondition&gt;()
///             .Map&lt;PaymentMapper&gt;()
///             .Transform&lt;EnrichmentTransform&gt;()
///             .Export&lt;EmailExporter&gt;();
///     }
/// }
/// </code>
/// </example>
/// <remarks>
/// <para>
/// Config classes are registered in DI as transient services, so they may take
/// constructor dependencies; those dependencies are injected when the sector
/// graph is first materialized.
/// </para>
/// </remarks>
public interface INotificationSectorConfig<TNotification, TEvent>
{
    /// <summary>
    /// Execution mode for this sector. Defaults to <see cref="SectorExecution.Auto"/>,
    /// which picks the compiled hot path when every node is compile-safe
    /// (singleton-registered or stateless) and otherwise falls back to the scoped
    /// path with a logged reason. <see cref="SectorExecution.Compiled"/> forces the
    /// compiled path and fails fast on scoped dependencies.
    /// </summary>
    SectorExecution Execution => SectorExecution.Auto;

    /// <summary>
    /// Sector-level default <see cref="BranchPolicy"/> applied to every
    /// <c>Branch</c> fan-out that does not carry its own policy override.
    /// <see langword="null"/> (the default) means <see cref="BranchPolicy.FailFast"/>.
    /// </summary>
    BranchPolicy? DefaultBranchPolicy => null;

    /// <summary>
    /// Describe the sector graph: conditions, the map node and the
    /// post-map node walk (transforms, branches, joins, custom nodes, exporters).
    /// </summary>
    /// <param name="graph">builder recording the graph in fluent call order</param>
    void Configure(ISectorGraphBuilder<TNotification, TEvent> graph);
}
