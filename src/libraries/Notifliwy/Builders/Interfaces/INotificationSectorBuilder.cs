using Notifliwy.Contexts.Interfaces;

namespace Notifliwy.Builders.Interfaces;

/// <summary>
/// Base notification block by assigned
/// </summary>
public interface INotificationSectorBuilder
{
    /// <summary>
    /// Collect all added service to <see cref="INotificationSector{TNotification,TEvent}"/>
    /// </summary>
    public void RegisterSector();
}