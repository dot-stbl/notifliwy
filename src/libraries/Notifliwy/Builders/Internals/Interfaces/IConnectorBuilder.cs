using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Connectors;

namespace Notifliwy.Builders.Internals.Interfaces;

/// <summary>
/// <see cref="NotificationConnectorService{TEvent}"/> builder
/// </summary>
public interface IConnectorBuilder
{
    /// <summary>
    /// Register all current connector by <c>event</c> type
    /// </summary>
    public void BuildConnector(IServiceCollection serviceCollection);
}