using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Notifliwy.Graph.Internals;

/// <summary>
/// Registration entry for <see cref="SectorGraphExecutor{TNotification,TEvent}"/>.
/// The factory registration passes the executor the root provider plus the
/// live <see cref="IServiceCollection"/> captured from registration time, so the
/// startup path selection can analyze node lifetimes against the final
/// descriptor list and resolve singleton node instances once.
/// </summary>
internal static class SectorGraphExecutorRegistration
{
    /// <summary>
    /// Register the graph executor singleton for one sector graph.
    /// </summary>
    /// <typeparam name="TNotification">notification type produced by the graph <c>Map</c> node</typeparam>
    /// <typeparam name="TEvent">event type consumed by the sector</typeparam>
    public static IServiceCollection AddSectorGraphExecutor<TNotification, TEvent>(
        this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<SectorGraphExecutor<TNotification, TEvent>>(serviceProvider =>
            new SectorGraphExecutor<TNotification, TEvent>(
                serviceProvider.GetRequiredService<SectorGraphPlan<TNotification, TEvent>>(),
                serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                serviceProvider,
                serviceCollection,
                serviceProvider.GetService<ILogger<SectorGraphExecutor<TNotification, TEvent>>>()));

        return serviceCollection;
    }
}
