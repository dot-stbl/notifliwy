using System;

namespace Notifliwy.Extensions.System;

/// <summary>
/// <c>System</c>.<see cref="Random"/> extensions
/// </summary>
public static class RandomExtensions
{
    /// <summary>
    /// Get random value from enum collection 
    /// </summary>
    internal static TEnum? NextEnum<TEnum>()
    {
        var enumArray = Enum.GetValues(typeof(TEnum));
        return (TEnum?)enumArray.GetValue(index: Random.Shared.Next(enumArray.Length));
    }
}