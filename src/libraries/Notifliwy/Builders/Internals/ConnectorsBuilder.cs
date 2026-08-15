using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Builders.Internals.Interfaces;
using Notifliwy.Connectors;

namespace Notifliwy.Builders.Internals;

/// <inheritdoc />
internal class ConnectorsBuilder<TEvent> : IConnectorBuilder
{
    /// <inheritdoc />
    public void BuildConnector(IServiceCollection serviceCollection)
    {
        if (serviceCollection.FirstOrDefault(descriptor
                    => descriptor.ImplementationType == typeof(NotificationConnector<TEvent>)) == null)
        {
            serviceCollection.AddHostedService<NotificationConnector<TEvent>>();
        }
    }
}