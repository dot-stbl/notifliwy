using Notifliwy.Config;
using Notifliwy.Config.Interfaces;
using Notifliwy.Graph;
using Notifliwy.Graph.Interfaces;
using Notifliwy.Sample.Kafka;

namespace Notifliwy.Sample.Kafka.Server;

/// <summary>
/// Sector described as a config class: When → Map → Transform → Branch(console | database).
/// The fan-out runs with <see cref="BranchPolicy.BestEffort"/> — a failed database write is
/// logged and skipped while the console delivery still counts (the default is
/// <see cref="BranchPolicy.FailFast"/>, where the first branch fault rethrows).
/// </summary>
/// <remarks>
/// The database exporter resolves a scoped <c>TempDbContext</c>, so the sector's default
/// <see cref="SectorExecution.Auto"/> mode falls back to the per-event scoped path at startup —
/// exactly what <c>Auto</c> is for.
/// </remarks>
public class CatMeowSector : INotificationSectorConfig<CatMeowNotification, CatMeowEvent>
{
    /// <inheritdoc />
    public void Configure(ISectorGraphBuilder<CatMeowNotification, CatMeowEvent> graph)
    {
        graph
            .When<CatMeowCondition>()
            .Map<CatMeowMapper>()

            // Transforms run sequentially, each receiving the previous result.
            .Transform<ColorNotificationTransform>()
            .Transform<ConstantColorNotificationTransform>()

            // two independent deliveries of the same notification
            .Branch(
                BranchPolicy.BestEffort,
                branch => branch.Export<CatNotificationConsoleExporter>(),
                branch => branch.Export<CatNotificationDatabaseExporter>());
    }
}
