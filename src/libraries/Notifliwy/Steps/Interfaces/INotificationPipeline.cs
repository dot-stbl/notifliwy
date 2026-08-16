using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Extensions;
using Notifliwy.Transform.Interfaces;

namespace Notifliwy.Steps.Interfaces;

/// <summary>
/// Assigned notification pipeline with <see cref="INotificationTransform{TNotification}"/>'s
/// </summary>
public interface INotificationPipeline<TNotification>
{
    /// <summary>
    /// Assigned transforms
    /// </summary>
    public IReadOnlyCollection<INotificationTransform<TNotification>> CurrentTransforms { get; }

    /// <summary>
    /// Invoke pipeline processing by <see cref="CurrentTransforms"/>
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
    /// Compile default action by <paramref name="currentTransforms"/>
    /// </summary>
    /// <param name="currentTransforms"></param>
    public NotificationPipeline(INotificationTransform<TNotification>[] currentTransforms)
    {
        CurrentTransforms = currentTransforms;

        if (currentTransforms.Length == 0)
        {
            CompiledPipeline = (notification, _) => new ValueTask<TNotification>(notification);
        }
        else
        {
            CompiledPipeline = (notification, token) =>
                CurrentTransforms.AggregateAsync(
                    notification,
                    (aggregateNotification, transform) =>
                        transform.TransformAsync(aggregateNotification, token));
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<INotificationTransform<TNotification>> CurrentTransforms { get; }

    /// <summary>
    /// Compiled function for <see cref="INotificationTransform{TNotification}"/>
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
