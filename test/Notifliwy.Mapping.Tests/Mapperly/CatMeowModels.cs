using Notifliwy.Mapping.Mapperly;
using Riok.Mapperly.Abstractions;

namespace Notifliwy.Mapping.Tests.Mapperly;

/// <summary>
/// Shared payload types and the real Mapperly-generated mapper for the adapter tests.
/// </summary>
public sealed class CatMeowEvent
{
    /// <summary>
    /// Loudness of the meow.
    /// </summary>
    public int Volume { get; init; }
}

/// <summary>
/// Notification projected from <see cref="CatMeowEvent"/>.
/// </summary>
public sealed class CatMeowNotification
{
    /// <summary>
    /// Loudness carried over from the event.
    /// </summary>
    public int Volume { get; set; }
}

/// <summary>
/// Closed mapping contract the Mapperly generator implements.
/// </summary>
public interface ICatMeowMapping : IMapperlyNotificationMapping<CatMeowNotification, CatMeowEvent>;

/// <summary>
/// Mapperly-generated mapper: the <c>ToNotification</c> body is produced by the source
/// generator at compile time (flows transitively from the adapter package).
/// </summary>
[Mapper]
public sealed partial class CatMeowMapper : ICatMeowMapping
{
    /// <summary>
    /// Generated mapping: same-name property projection.
    /// </summary>
    public partial CatMeowNotification ToNotification(CatMeowEvent inputEvent);
}

/// <summary>
/// One-line subclass pinning the closed generics to a short graph-usable name.
/// </summary>
public sealed class CatMeowNotificationMapper()
    : MapperlyNotificationMapper<CatMeowNotification, CatMeowEvent, CatMeowMapper>(new CatMeowMapper());
