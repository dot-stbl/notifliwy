using System;
using System.Linq;
using Notifliwy.Steps.Interfaces;
using Notifliwy.Transform.Interfaces;
using System.Collections.Generic;
using Notifliwy.Builders.Internals.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Notifliwy.Builders.Internals;

/// <summary>
/// Global stages builder for assigned <c>Notification</c> type
/// </summary>
public class PipelineBuilder<TNotification> : IStagesBuilder
{
    /// <summary>
    /// Current linked transforms
    /// </summary>
    private IList<Type> LinkedTransforms { get; } = [];

    /// <summary>
    /// Add <typeparamref name="TTransform"/> to stages of processing <c>notification</c>
    /// </summary>
    public PipelineBuilder<TNotification> AddStep<TTransform>()
            where TTransform : INotificationTransform<TNotification>
    {
        LinkedTransforms.Add(typeof(TTransform));
        return this;
    }

    /// <inheritdoc />
    public void BuildPipeline(IServiceCollection serviceCollection)
    {
        var transformTypes = LinkedTransforms.ToArray();

        if (transformTypes.Length == 0)
        {
            return;
        }

        foreach (var transformType in transformTypes)
        {
            serviceCollection.AddScoped(transformType);
        }

        serviceCollection.AddScoped(
            typeof(INotificationPipeline<TNotification>),
            provider =>
            {
                var assignedTransforms = transformTypes
                        .Select(provider.GetRequiredService)
                        .Cast<INotificationTransform<TNotification>>()
                        .ToArray();

                return new NotificationPipeline<TNotification>(assignedTransforms);
            });
    }
}
