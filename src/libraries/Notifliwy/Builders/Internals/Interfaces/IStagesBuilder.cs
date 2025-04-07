using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Steps.Interfaces;

namespace Notifliwy.Builders.Internals.Interfaces;

internal interface IStagesBuilder
{
    /// <summary>
    /// Register all assigned <see cref="INotificationStep{TNotification}"/>
    /// </summary>
    public void BuildPipeline(IServiceCollection serviceCollection);
}