using System;
using System.Collections.Generic;
using Notifliwy.Related;

namespace Notifliwy.Extensions.Dependency;

/// <summary>
/// Extensions for <see cref="MultiplyServiceInstance{TInstance}"/> and <see cref="IServiceProvider"/>
/// </summary>
internal static class MultiplyProviderExtensions
{
    /// <summary>
    /// Cast <paramref name="instances"/> to <c>instances</c>
    /// </summary>
    /// <param name="instances">input instances types</param>
    /// <typeparam name="TInstance">some type</typeparam>
    public static MultiplyServiceInstance<TInstance> ToMultiplyService<TInstance>(
        this IEnumerable<TInstance> instances)
    {
        return new MultiplyServiceInstance<TInstance>(instances);
    }
}