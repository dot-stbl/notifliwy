using System;
using System.Linq;
using Notifliwy.Config.Interfaces;

namespace Notifliwy.Config.Internals;

/// <summary>
/// Resolves the closed <see cref="INotificationSectorConfig{TNotification,TEvent}"/>
/// contract implemented by a configuration class.
/// </summary>
internal static class SectorConfigContract
{
    /// <summary>
    /// Find the closed config interface on <paramref name="configType"/> and
    /// return its generic arguments. Throws <see cref="InvalidOperationException"/>
    /// when the class does not implement the contract.
    /// </summary>
    /// <param name="configType">sector configuration class</param>
    /// <returns>notification and event types of the closed contract</returns>
    public static (Type NotificationType, Type EventType) Resolve(Type configType)
    {
        var interfaceType = configType
            .GetInterfaces()
            .SingleOrDefault(candidate => candidate.IsGenericType
                    && candidate.GetGenericTypeDefinition() == typeof(INotificationSectorConfig<,>));

        if (interfaceType is null)
        {
            throw new InvalidOperationException(
                $"{configType.Name} must implement INotificationSectorConfig<TNotification, TEvent> "
                + "to be registered via AddSector<TConfig>");
        }

        var arguments = interfaceType.GetGenericArguments();

        return (arguments[0], arguments[1]);
    }
}
