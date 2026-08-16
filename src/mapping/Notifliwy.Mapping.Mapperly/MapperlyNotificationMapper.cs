using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Mapper.Interfaces;

namespace Notifliwy.Mapping.Mapperly;

/// <summary>
/// Adapts a Mapperly-generated mapper into the Notifliwy
/// <see cref="INotificationMapper{TNotification, TEvent}"/> contract, so a
/// compile-time generated mapping plugs into a sector graph <c>Map</c> node without a
/// hand-written adapter.
/// </summary>
/// <typeparam name="TNotification">The resulting notification type</typeparam>
/// <typeparam name="TEvent">The incoming event type to convert</typeparam>
/// <typeparam name="TMapper">The Mapperly-generated mapper implementing <see cref="IMapperlyNotificationMapping{TNotification, TEvent}"/></typeparam>
/// <example>
/// <code>
/// public sealed class CatMeowNotificationMapper()
///     : MapperlyNotificationMapper&lt;CatMeowNotification, CatMeowEvent, CatMapper&gt;(new CatMapper());
///
/// graph.Map&lt;CatMeowNotificationMapper&gt;();
/// </code>
/// </example>
/// <remarks>
/// <para>Intentionally left open (not sealed): a derived class pins the closed generic
/// arguments so the adapter can be referenced by a short name in a sector graph
/// (<c>Map&lt;CatMeowNotificationMapper&gt;()</c>) and in DI registrations. When subclassing
/// is not desired, register the closed generic type directly via
/// <see cref="MapperlyNotificationMappingExtensions.AddNotifliwyMapperlyMapping{TNotification, TEvent, TMapper}"/>.</para>
/// <para>The wrapped Mapperly mapper is stateless and thread-safe; register the
/// adapter as a singleton.</para>
/// </remarks>
public class MapperlyNotificationMapper<TNotification, TEvent, TMapper>(TMapper mapper)
    : INotificationMapper<TNotification, TEvent>
    where TMapper : class, IMapperlyNotificationMapping<TNotification, TEvent>
{
    /// <summary>
    /// Converts the input event into the notification by delegating to the
    /// Mapperly-generated <see cref="IMapperlyNotificationMapping{TNotification, TEvent}.ToNotification"/>.
    /// </summary>
    /// <param name="inputEvent">The incoming event to convert</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests; the generated mapping is synchronous and does not observe it</param>
    /// <returns>A task representing the converted notification</returns>
    public ValueTask<TNotification> ConvertAsync(TEvent inputEvent, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(mapper.ToNotification(inputEvent));
    }
}
