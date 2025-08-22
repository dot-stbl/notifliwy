using System;
using System.Linq;
using Notifliwy.Steps.Interfaces;
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
    /// Current linked steps
    /// </summary>
    private IList<Type> LinkedSteps { get; } = [];

    /// <summary>
    /// Add <typeparamref name="TStep"/> to stages of processing <c>notification</c>
    /// </summary>
    public PipelineBuilder<TNotification> AddStep<TStep>()
        where TStep : INotificationStep<TNotification>
    {
        LinkedSteps.Add(item: typeof(TStep));
        return this;
    }

    /// <inheritdoc />
    public void BuildPipeline(IServiceCollection serviceCollection)
    {
        var stepTypes = LinkedSteps.ToArray();

        if (stepTypes.Length == 0)
        {
            return;
        }

        foreach (var stepType in stepTypes)
        {
            serviceCollection.AddScoped(stepType);
        }

        serviceCollection.AddScoped(
            serviceType: typeof(INotificationPipeline<TNotification>),
            implementationFactory: provider =>
            {
                var assignedSteps = stepTypes
                    .Select(provider.GetRequiredService)
                    .Cast<INotificationStep<TNotification>>()
                    .ToArray();

                return new NotificationPipeline<TNotification>(assignedSteps);
            });
    }
}