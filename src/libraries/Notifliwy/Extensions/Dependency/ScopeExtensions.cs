using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Contexts;
using Notifliwy.Contexts.Interfaces;

namespace Notifliwy.Extensions.Dependency;

/// <summary>
/// <see cref="IServiceScope"/> extensions
/// </summary>
internal static class ScopeExtensions
{
    /// <summary>
    /// Return instance of <see cref="SectorBlock{TNotification,TEvent}"/>
    /// </summary>
    public static AsyncServiceScope BlockBy<TNotification, TEvent>(
        this AsyncServiceScope serviceScope,
        out SectorBlock<TNotification, TEvent> sectorBlock)
    {
        sectorBlock = serviceScope.ServiceProvider.GetRequiredService<SectorBlock<TNotification, TEvent>>();
        return serviceScope;
    }

    /// <summary>
    /// Return assigned <see cref="INotificationSector{TEvent}"/> by <typeparamref name="TEvent"/>
    /// </summary>
    public static AsyncServiceScope SectorBy<TEvent>(
        this AsyncServiceScope serviceScope,
        out INotificationSector<TEvent>[] sectors)
    {
        sectors = serviceScope.ServiceProvider.GetServices<INotificationSector<TEvent>>().ToArray();
        return serviceScope;
    }
}