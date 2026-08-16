using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notifliwy.Mapper.Interfaces;

namespace Notifliwy.Mapping.Mapster;

/// <summary>
/// DI registration glue for Mapster-backed notification mappers.
/// </summary>
public static class MapsterNotificationMappingExtensions
{
    /// <summary>
    /// Registers the shared <see cref="TypeAdapterConfig"/> as a singleton and applies an
    /// optional configuration callback to it, making the configuration available to every
    /// <see cref="MapsterNotificationMapper{TNotification, TEvent}"/> resolved from DI.
    /// </summary>
    /// <param name="services">service collection to register the configuration into</param>
    /// <param name="configure">optional callback mutating the shared Mapster configuration</param>
    /// <example>
    /// <code>
    /// services.AddNotifliwyMapsterMapping(configure: config =>
    ///     config.NewConfig&lt;CatMeowEvent, CatMeowNotification&gt;());
    /// </code>
    /// </example>
    public static IServiceCollection AddNotifliwyMapsterMapping(
        this IServiceCollection services,
        Action<TypeAdapterConfig>? configure = null)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        configure?.Invoke(config);
        services.TryAddSingleton(config);

        return services;
    }

    /// <summary>
    /// Registers <see cref="MapsterNotificationMapper{TNotification, TEvent}"/> as the singleton
    /// <see cref="INotificationMapper{TNotification, TEvent}"/> for the given event and
    /// notification pair, usable by <c>Map&lt;MapsterNotificationMapper&lt;...&gt;&gt;()</c>.
    /// </summary>
    /// <typeparam name="TNotification">The resulting notification type</typeparam>
    /// <typeparam name="TEvent">The incoming event type to convert</typeparam>
    /// <example>
    /// <code>
    /// services.AddNotifliwyMapsterMapping&lt;CatMeowNotification, CatMeowEvent&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddNotifliwyMapsterMapping<TNotification, TEvent>(
        this IServiceCollection services)
    {
        services.TryAddSingleton<INotificationMapper<TNotification, TEvent>, MapsterNotificationMapper<TNotification, TEvent>>();

        return services;
    }
}
