using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Builders.Internals.Interfaces;
using Notifliwy.Models.Interfaces;
using Notifliwy.Steps.Interfaces;

namespace Notifliwy.Builders.Internals;

/// <summary>
/// Global stages builder for assigned <see cref="INotification"/> type
/// </summary>
public class PipelineBuilder<TNotification> : IStagesBuilder 
    where TNotification : INotification
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
        foreach (var stepType in LinkedSteps.ToArray())
        {
            serviceCollection.AddScoped(
                serviceType: typeof(INotificationStep<TNotification>),
                implementationType: stepType);
        }
    }
}