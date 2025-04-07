using System;
using System.Collections.Generic;

namespace Notifliwy.Extensions;

internal static class LinqExtensions
{
    /// <summary>
    /// Add to <paramref name="sources"/> and invoke <paramref name="actionAfter"/>
    /// </summary>
    public static void AddAction<TSource>(
        this IList<TSource> sources,
        TSource source,
        Action<TSource> actionAfter)
    {
        sources.Add(source);
        actionAfter.Invoke(source);
    }
}