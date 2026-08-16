using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Contexts.Interfaces;

namespace Notifliwy.Extensions.Dependency;

/// <summary>
/// <see cref="IServiceScope"/> extensions
/// </summary>
internal static class ScopeExtensions
{
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
