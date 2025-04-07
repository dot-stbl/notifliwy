using System;
using Notifliwy.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace Notifliwy.Dependency;

/// <summary>
/// Global registration logic <see cref="Notifliwy"/> server
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Add <see cref="NotificationServerBuilder"/> and child builder to <paramref name="serviceCollection"/>
    /// </summary>
    public static IServiceCollection AddNotifliwyServer(
        this IServiceCollection serviceCollection,
        Action<NotificationServerBuilder> configureServer)
    {
        var serverBuilder = NotificationServerBuilder.CreateInstance(serviceCollection);
        configureServer.Invoke(serverBuilder);
        serverBuilder.BuildServer();
        return serviceCollection;
    }
}