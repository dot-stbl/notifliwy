using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notifliwy.Mapper.Interfaces;

namespace Notifliwy.Mapping.Mapperly;

/// <summary>
/// DI registration glue for Mapperly-backed notification mappers.
/// </summary>
public static class MapperlyNotificationMappingExtensions
{
    /// <summary>
    /// Registers a Mapperly-generated <typeparamref name="TMapper"/> as the singleton
    /// <see cref="INotificationMapper{TNotification, TEvent}"/> for the given event and
    /// notification pair, usable by <c>Map&lt;MapperlyNotificationMapper&lt;...&gt;&gt;()</c>.
    /// </summary>
    /// <typeparam name="TNotification">The resulting notification type</typeparam>
    /// <typeparam name="TEvent">The incoming event type to convert</typeparam>
    /// <typeparam name="TMapper">The Mapperly-generated mapper implementing <see cref="IMapperlyNotificationMapping{TNotification, TEvent}"/></typeparam>
    /// <example>
    /// <code>
    /// services.AddNotifliwyMapperlyMapping&lt;CatMeowNotification, CatMeowEvent, CatMapper&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddNotifliwyMapperlyMapping<TNotification, TEvent, TMapper>(
        this IServiceCollection services)
        where TMapper : class, IMapperlyNotificationMapping<TNotification, TEvent>
    {
        services.TryAddSingleton<TMapper>();
        services.TryAddSingleton<INotificationMapper<TNotification, TEvent>, MapperlyNotificationMapper<TNotification, TEvent, TMapper>>();

        return services;
    }
}
