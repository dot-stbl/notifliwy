namespace Notifliwy.Mapping.Mapperly;

/// <summary>
/// Bridge contract between a Mapperly-generated mapper and Notifliwy mapping nodes.
/// Declare a closed interface inheriting this contract and let a <c>[Mapper]</c> partial
/// class implement it — the Mapperly source generator writes the mapping body at compile time.
/// </summary>
/// <typeparam name="TNotification">The resulting notification type</typeparam>
/// <typeparam name="TEvent">The incoming event type to convert</typeparam>
/// <example>
/// <code>
/// public interface ICatMapping : IMapperlyNotificationMapping&lt;CatMeowNotification, CatMeowEvent&gt;;
///
/// [Mapper]
/// public sealed partial class CatMapper : ICatMapping
/// {
///     public partial CatMeowNotification ToNotification(CatMeowEvent inputEvent);
/// }
/// </code>
/// </example>
public interface IMapperlyNotificationMapping<TNotification, in TEvent>
{
    /// <summary>
    /// Converts the input event into the notification. The body is generated
    /// by the Mapperly source generator at compile time.
    /// </summary>
    /// <param name="inputEvent">The incoming event to convert</param>
    /// <returns>The mapped notification instance</returns>
    TNotification ToNotification(TEvent inputEvent);
}
