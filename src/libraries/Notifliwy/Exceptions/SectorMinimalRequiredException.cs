using System;
using Notifliwy.Contexts;

namespace Notifliwy.Exceptions;

/// <summary>
/// Exception failed build <see cref="SectorBlock{TNotification,TEvent}"/>
/// </summary>
public class SectorMinimalRequiredException(string typeRequired)
        : Exception($"Required service {typeRequired} is not registered");