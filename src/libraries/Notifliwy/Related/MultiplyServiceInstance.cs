using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Exceptions;

namespace Notifliwy.Related;

/// <summary>
/// Accepts one type of service and provides functionality to determine how many services,
/// also has an api for checks
/// </summary>
/// <typeparam name="TInstance">some service type</typeparam>
public class MultiplyServiceInstance<TInstance>
{
    /// <summary>
    /// Single service instance
    /// </summary>
    public TInstance? Single { get; init; }

    /// <summary>
    /// Multiply service instances
    /// </summary>
    public TInstance[]? Multiply { get; init; }

    /// <summary>
    /// Checks to see if there is at least one instance
    /// </summary>
    public bool IsSingle => Single != null;

    /// <summary>
    /// Checks if plural instances are being used
    /// </summary>
    public bool IsMultiply => Multiply != null;

    /// <summary>
    /// Are there any services in this block at all
    /// </summary>
    public bool UseInstance => Single != null || Multiply?.Length > 0;

    /// <summary>
    /// It gets the required type of instances from <paramref name="serviceProvider"/> and fills the block itself
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/></param>
    public MultiplyServiceInstance(IServiceProvider serviceProvider)
    {
        var instances = serviceProvider
                .GetServices<TInstance>()
                .ToArray();

        if (instances.Length == 1 && instances.FirstOrDefault() is { } instance)
        {
            Single = instance;
        }
        else
        {
            Multiply = instances;
        }
    }

    /// <summary>
    /// Accepts an already ready collection of types and fills the block
    /// </summary>
    /// <param name="instanceEnumerable">collection of instances like</param>
    public MultiplyServiceInstance(IEnumerable<TInstance> instanceEnumerable)
    {
        var instances = instanceEnumerable as TInstance[] ?? instanceEnumerable.ToArray();

        if (instances.Length == 1 && instances.FirstOrDefault() is { } instance)
        {
            Single = instance;
        }
        else
        {
            Multiply = instances;
        }
    }

    /// <summary>
    /// Protected empty constructor
    /// </summary>
    protected MultiplyServiceInstance() { }

    /// <summary>
    /// Return nullable <see cref="MultiplyServiceInstance{TInstance}"/>
    /// </summary>
    public static MultiplyServiceInstance<TInstance> Nullable => new()
    {
        Multiply = null,
        Single = default
    };

    /// <summary>
    /// Compute current class and invoke by single and multiply instances logic async
    /// </summary>
    public async ValueTask<TResult> CheckoutInstanceAsync<TResult>(
        Func<TInstance, ValueTask<TResult>> singleAction,
        Func<TInstance[], ValueTask<TResult>> multiplyAction)
    {
        if (!UseInstance)
        {
            throw new EmptyInstanceBranchException(typeof(TInstance));
        }

        if (IsSingle && Single != null)
        {
            return await singleAction(Single);
        }

        return await multiplyAction(Multiply!);
    }

    /// <summary>
    /// Compute current class and invoke by single and multiply instances logic async
    /// </summary>
    public async ValueTask CheckoutInstanceAsync(
        Func<TInstance, ValueTask> singleAction,
        Func<TInstance[], ValueTask> multiplyAction)
    {
        if (!UseInstance)
        {
            throw new EmptyInstanceBranchException(typeof(TInstance));
        }

        if (IsSingle && Single != null)
        {
            await singleAction(Single);
            return;
        }

        await multiplyAction(Multiply!);
    }
}