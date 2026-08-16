using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Builders.Internals;
using Notifliwy.Builders.Internals.Interfaces;
using Notifliwy.Config;
using Notifliwy.Config.Internals;
using Notifliwy.Config.Interfaces;
using Notifliwy.Connectors;
using Notifliwy.Contexts;
using Notifliwy.Contexts.Interfaces;
using Notifliwy.Graph;
using Notifliwy.Graph.Internals;
using Notifliwy.Graph.Interfaces;
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
    /// All <see cref="ConnectorsBuilder{TEvent}"/> for this <see cref="Notifliwy"/> server
    /// </summary>
    internal HashSet<IConnectorBuilder> ConnectorsBuilders { get; } = [];

    /// <summary>
    /// Register a sector from its configuration class: the class is registered in DI
    /// (transient, so it may take constructor dependencies), its graph is materialized
    /// and validated when the sector is first resolved — at connector startup for a
    /// hosted server — and a <see cref="NotificationConnector{TEvent}"/> is wired for
    /// the bound event type.
    /// </summary>
    /// <typeparam name="TConfig">sector configuration class</typeparam>
    public NotificationServerBuilder AddSector<TConfig>()
            where TConfig : class
    {
        var (notificationType, eventType) = SectorConfigContract.Resolve(typeof(TConfig));

        AddConfiguredSectorMethod
            .MakeGenericMethod(notificationType, eventType, typeof(TConfig))
            .Invoke(this, []);

        return this;
    }

    /// <summary>
    /// Register a one-off sector from an inline graph lambda. The graph is built,
    /// validated and registered immediately, so a broken structure fails at
    /// registration time.
    /// </summary>
    /// <typeparam name="TNotification">notification type produced by the graph <c>Map</c> node</typeparam>
    /// <typeparam name="TEvent">event type consumed by the sector</typeparam>
    /// <param name="graph">inline sector graph configuration</param>
    public NotificationServerBuilder AddSector<TNotification, TEvent>(
        Action<ISectorGraphBuilder<TNotification, TEvent>> graph)
    {
        var graphBuilder = new SectorGraphBuilder<TNotification, TEvent>();
        graph.Invoke(graphBuilder);
        graphBuilder.RegisterGraph(serviceCollection);

        RegisterSectorServices<TNotification, TEvent>();

        return this;
    }

    /// <summary>
    /// Register a sector from a configuration class instance contract: config class in DI
    /// as transient, plan singleton materialized from the resolved config (honouring its
    /// <see cref="INotificationSectorConfig{TNotification,TEvent}.Execution"/> and
    /// <see cref="INotificationSectorConfig{TNotification,TEvent}.DefaultBranchPolicy"/>),
    /// graph executor, sector and connector.
    /// </summary>
    /// <typeparam name="TNotification">notification type produced by the graph <c>Map</c> node</typeparam>
    /// <typeparam name="TEvent">event type consumed by the sector</typeparam>
    /// <typeparam name="TConfig">sector configuration class</typeparam>
    internal NotificationServerBuilder AddConfiguredSector<TNotification, TEvent, TConfig>()
            where TConfig : class, INotificationSectorConfig<TNotification, TEvent>
    {
        serviceCollection.AddTransient<TConfig>();

        serviceCollection.AddSingleton<SectorGraphPlan<TNotification, TEvent>>(serviceProvider =>
        {
            var config = serviceProvider.GetRequiredService<TConfig>();
            var graphBuilder = new SectorGraphBuilder<TNotification, TEvent>();
            config.Configure(graphBuilder);

            return graphBuilder.BuildPlan(config.DefaultBranchPolicy, config.Execution);
        });

        serviceCollection.AddSingleton<SectorGraphExecutor<TNotification, TEvent>>();

        RegisterSectorServices<TNotification, TEvent>();

        return this;
    }

    /// <summary>
    /// Register the sector service and its connector for the bound event type
    /// </summary>
    private void RegisterSectorServices<TNotification, TEvent>()
    {
        //as full generic
        serviceCollection.AddTransient(
            typeof(INotificationSector<TEvent>),
            typeof(NotificationSector<TNotification, TEvent>));

        ConnectorsBuilders.Add(new ConnectorsBuilder<TEvent>());
    }

    #region InputPipes

    /// <summary>
    /// Add default <c>in memory</c> transform channel logic
    /// </summary>
    public NotificationServerBuilder AddInMemoryInput(
        Action<InMemoryExchangeOptions>? configureExchange = null)
    {
        var optionsBuilder = serviceCollection
            .AddOptions<InMemoryExchangeOptions>();

        if (configureExchange != null)
        {
            optionsBuilder.Configure(configureExchange);
        }

        serviceCollection.AddSingleton(
            implementationType: typeof(InMemoryEventExchange<>),
            serviceType: typeof(IInMemoryEventExchange<>));

        serviceCollection.AddTransient(
            implementationType: typeof(InMemoryExportPipe<>),
            serviceType: typeof(IExportPipe<>));

        serviceCollection.AddTransient(
            implementationType: typeof(InMemoryInputPipe<>),
            serviceType: typeof(IInputPipe<>));

        return this;
    }

    #endregion

    /// <summary>
    /// Build all registered <see cref="ConnectorsBuilder{TEvent}"/>
    /// </summary>
    internal IServiceCollection BuildServer()
    {
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

    private static readonly MethodInfo AddConfiguredSectorMethod = typeof(NotificationServerBuilder)
            .GetMethod(nameof(AddConfiguredSector), BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException(
            $"{nameof(AddConfiguredSector)} generic registration entry point is missing");
}
