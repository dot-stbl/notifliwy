using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Builders.Internals.Interfaces;
using Notifliwy.Connectors;
using Notifliwy.Handlers.Interfaces;
using Notifliwy.Models.Interfaces;
using Notifliwy.Options;

namespace Notifliwy.Builders.Internals;

/// <inheritdoc />
internal class ConnectorsBuilder<TEvent> : IConnectorBuilder where TEvent : IEvent
{
    /// <inheritdoc />
    public void BuildConnector(IServiceCollection serviceCollection)
    {
        if (serviceCollection.FirstOrDefault(descriptor 
                => descriptor.ImplementationType == typeof(NotificationConnectorService<TEvent>)) == null)
        {
            serviceCollection.AddSingleton(new NotificationConnectorOptions<TEvent>
            {
                WorkerCount = 4
            });
            
            serviceCollection.AddHostedService<NotificationConnectorService<TEvent>>();
        }
        
        serviceCollection.AddScoped<INotificationHandler<TEvent>[]>(provider => 
            provider.GetServices<INotificationHandler<TEvent>>().ToArray()
        );
    }
}