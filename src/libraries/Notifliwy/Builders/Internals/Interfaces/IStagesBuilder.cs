using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Steps.Interfaces;
using Notifliwy.Transform.Interfaces;

namespace Notifliwy.Builders.Internals.Interfaces;

internal interface IStagesBuilder
{
    /// <summary>
    /// Register all assigned <see cref="INotificationTransform{TNotification}"/>
    /// </summary>
    public void BuildPipeline(IServiceCollection serviceCollection);
}