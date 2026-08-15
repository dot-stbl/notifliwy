using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Notifliwy.Extensions;

/// <summary>
/// Project <see cref="IEnumerable{T}"/> extensions
///     -> <c>async</c> versions
/// </summary>
internal static class EnumerableExtensions
{
    /// <summary>
    /// Asynchronously aggregates sequence elements by applying an asynchronous accumulator function.
    /// </summary>
    public async static ValueTask<TAccumulate> AggregateAsync<TSource, TAccumulate>(
        this IEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, ValueTask<TAccumulate>> func)
    {
        var accumulator = seed;

        foreach (var item in source)
        {
            accumulator = await func(accumulator, item);
        }

        return accumulator;
    }
}