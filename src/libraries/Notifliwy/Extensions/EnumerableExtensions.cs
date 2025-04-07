using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Notifliwy.Extensions;

internal static class EnumerableExtensions
{
    /// <summary>
    /// Asynchronously aggregates sequence elements by applying an asynchronous accumulator function.
    /// </summary>
    public static async ValueTask<TAccumulate> AggregateAsync<TSource, TAccumulate>(
        this IEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, Task<TAccumulate>> func)
    {
        var accumulator = seed;
        
        foreach (var item in source)
        {
            accumulator = await func(accumulator, item);
        }

        return accumulator;
    }
}