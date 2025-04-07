using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Builders.Interfaces;
using Notifliwy.Builders.Internals;
using Notifliwy.Builders.Internals.Interfaces;
using Notifliwy.Contexts.Compilers;
using Notifliwy.Models.Interfaces;
using Notifliwy.Pipes.InMemory;
using Notifliwy.Pipes.InMemory.Interfaces;
using Notifliwy.Pipes.InMemory.Options;
using Notifliwy.Pipes.Interfaces;

namespace Notifliwy.Builders;

/// <summary>
/// Main builder <c>Notifliwy</c> server
/// </summary>
public class NotificationServerBuilder(IServiceCollection serviceCollection)
{
    /// <summary>
    /// <c>Notification</c> type bound to an <c>event</c>
    /// </summary>
    protected Dictionary<Type, HashSet<Type>> AssignedNotifications { get; } = new();
    
    /// <summary>
    /// Added <see cref="INotificationSectorBuilder"/>
    /// </summary>
    protected IList<INotificationSectorBuilder> SectorBuilders { get; } = [];

    /// <summary>
    /// All <see cref="ConnectorsBuilder{TEvent}"/> for this <see cref="Notifliwy"/> server
    /// </summary>
    internal HashSet<IConnectorBuilder> ConnectorsBuilders { get; } = [];
    
    /// <summary>
    /// Add new assigned sector by <typeparamref name="TNotification"/> and <typeparamref name="TEvent"/> 
    /// </summary>
    public NotificationServerBuilder AddNotification<TNotification, TEvent>(
        Action<NotificationSectorBuilder<TNotification, TEvent>>? sectorBuilder = null) 
            where TNotification : INotification
            where TEvent : IEvent
    {
        var builder = new NotificationSectorBuilder<TNotification, TEvent>(serviceCollection);
        {
            SectorBuilders.Add(builder);
            sectorBuilder?.Invoke(builder);

            AssignedNotifications.AddBindings<TNotification, TEvent>();
        }
        
        ConnectorsBuilders.Add(item: new ConnectorsBuilder<TEvent>());
        
        return this;
    }

    #region InputPipes

    /// <summary>
    /// Add default <c>in memory</c> transform channel logic
    /// </summary>
    public NotificationServerBuilder AddInMemoryInput(
        Action<InMemoryExchangeOptions>? configureExchange = null)
    {
        if (configureExchange != null)
        {
            var exchangeOptions = new InMemoryExchangeOptions();
            configureExchange.Invoke(exchangeOptions);   
            serviceCollection.AddSingleton(exchangeOptions);
        }
        
        serviceCollection.AddSingleton(
            implementationType: typeof(InMemoryEventExchange<>),
            serviceType: typeof(IInMemoryEventExchange<>));

        serviceCollection.AddTransient(
            implementationType: typeof(InMemoryExportPipe<>),
            serviceType: typeof(IExportPipe<>));
        
        serviceCollection.AddTransient(
            implementationType: typeof(InMemoryInputPipe<>), 
            serviceType:typeof(IInputPipe<>));
        
        return this;
    }

    #endregion
    
    /// <summary>
    /// Build added <see cref="SectorBuilders"/> 
    /// </summary>
    internal IServiceCollection BuildServer()
    {
        foreach (var sectorBuilder in SectorBuilders)
        {
            sectorBuilder.RegisterSector();
        }
        
        foreach (var connectorsBuilder in ConnectorsBuilders)
        {
            connectorsBuilder.BuildConnector(serviceCollection);
        }
        
        return serviceCollection;
    }
    
    /// <summary>
    /// Create new instance of <see cref="NotificationServerBuilder"/>
    /// </summary>
    public static NotificationServerBuilder CreateInstance(IServiceCollection serviceCollection)
    {
        return new NotificationServerBuilder(serviceCollection);
    }
}