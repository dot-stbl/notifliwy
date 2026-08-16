namespace Notifliwy.Mapping.Tests.Mapster;

/// <summary>
/// Shared payload types for the Mapster adapter tests.
/// </summary>
public sealed class DogBarkEvent
{
    /// <summary>
    /// Loudness of the bark.
    /// </summary>
    public int Loudness { get; init; }
}

/// <summary>
/// Notification projected from <see cref="DogBarkEvent"/>.
/// </summary>
public sealed class DogBarkNotification
{
    /// <summary>
    /// Loudness carried over from the event.
    /// </summary>
    public int Loudness { get; set; }
}
