using System.Threading;
using System.Threading.Tasks;
using Mapster;
using Notifliwy.Mapper.Interfaces;

namespace Notifliwy.Mapping.Mapster;

/// <summary>
/// Adapts a Mapster <see cref="TypeAdapterConfig"/> rule into the Notifliwy
/// <see cref="INotificationMapper{TNotification, TEvent}"/> contract, so existing
/// Mapster mappings plug into a sector graph <c>Map</c> node without a hand-written adapter.
/// </summary>
/// <typeparam name="TNotification">The resulting notification type</typeparam>
/// <typeparam name="TEvent">The incoming event type to convert</typeparam>
/// <example>
/// <code>
/// var config = new TypeAdapterConfig();
/// config.NewConfig&lt;CatMeowEvent, CatMeowNotification&gt;();
///
/// graph.Map(new MapsterNotificationMapper&lt;CatMeowNotification, CatMeowEvent&gt;(config));
/// </code>
/// </example>
/// <remarks>
/// <para>The <see cref="MapsterNotificationMapper(TypeAdapterConfig)"/> constructor compiles
/// the mapping delegate once via <see cref="TypeAdapterConfig.GetMapFunction{TSource, TDestination}"/>;
/// every conversion is a plain compiled-delegate invocation with no per-event rule resolution.</para>
/// <para>Compiled Mapster delegates are thread-safe; register the adapter as a singleton.</para>
/// </remarks>
public sealed class MapsterNotificationMapper<TNotification, TEvent>(Func<TEvent, TNotification> mapping)
    : INotificationMapper<TNotification, TEvent>
{
    /// <summary>
    /// Constructs the adapter from a Mapster configuration, compiling the
    /// <typeparamref name="TEvent"/> to <typeparamref name="TNotification"/> mapping delegate up front.
    /// </summary>
    /// <param name="config">Mapster configuration holding the mapping rule; falls back to convention mapping when no explicit rule is registered</param>
    public MapsterNotificationMapper(TypeAdapterConfig config)
        : this(config.GetMapFunction<TEvent, TNotification>())
    {
    }

    /// <summary>
    /// Constructs the adapter from the global <see cref="TypeAdapterConfig.GlobalSettings"/>,
    /// the same configuration the <c>.AdaptToType&lt;TDestination&gt;()</c> extensions use.
    /// </summary>
    public MapsterNotificationMapper()
        : this(TypeAdapterConfig.GlobalSettings)
    {
    }

    /// <summary>
    /// Converts the input event into the notification by invoking the compiled Mapster delegate.
    /// </summary>
    /// <param name="inputEvent">The incoming event to convert</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests; the compiled mapping is synchronous and does not observe it</param>
    /// <returns>A task representing the converted notification</returns>
    public ValueTask<TNotification> ConvertAsync(TEvent inputEvent, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(mapping(inputEvent));
    }
}
