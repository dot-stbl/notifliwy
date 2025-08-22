using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Extensions;

namespace Notifliwy.Steps.Interfaces;

/// <summary>
/// Assigned notification pipeline with <see cref="INotificationStep{TNotification}"/>'s
/// </summary>
public interface INotificationPipeline<TNotification>
{
    /// <summary>
    /// Assigned steps
    /// </summary>
    public IReadOnlyCollection<INotificationStep<TNotification>> CurrentSteps { get; }

    /// <summary>
    /// Invoke pipeline processing by <see cref="CurrentSteps"/>
    /// </summary>
    public ValueTask<TNotification> InvokePipeline(
        TNotification notification,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Default <see cref="INotificationPipeline{TNotification}"/> executor service
/// </summary>
public class NotificationPipeline<TNotification> : INotificationPipeline<TNotification>
{
    /// <summary>
    /// Compile default action by <paramref name="currentSteps"/>
    /// </summary>
    /// <param name="currentSteps"></param>
    public NotificationPipeline(INotificationStep<TNotification>[] currentSteps)
    {
        CurrentSteps = currentSteps;

        if (currentSteps.Length == 0)
        {
            CompiledPipeline = (notification, _) => new ValueTask<TNotification>(notification);
        }
        else
        {
            CompiledPipeline = async (notification, token) =>
            {
                return await CurrentSteps.AggregateAsync(notification,
                    func: async (aggregateNotification, step) =>
                        await step.AggregateAsync(aggregateNotification, token));
            };
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<INotificationStep<TNotification>> CurrentSteps { get; }

    /// <summary>
    /// Compiled function for <see cref="INotificationStep{TNotification}"/>
    /// </summary>
    internal Func<TNotification, CancellationToken, ValueTask<TNotification>> CompiledPipeline { get; init; }

    /// <inheritdoc />
    public async ValueTask<TNotification> InvokePipeline(
        TNotification notification,
        CancellationToken cancellationToken = default)
    {
        return await CompiledPipeline(notification, cancellationToken);
    }
}