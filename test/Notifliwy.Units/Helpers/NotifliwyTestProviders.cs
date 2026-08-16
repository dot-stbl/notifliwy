using System;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Builders;
using Notifliwy.Pipes.InMemory.Options;

namespace Notifliwy.Units.Helpers;

/// <summary>
/// Shared factory for minimal Notifliwy service providers used by unit tests.
/// Registers the DI infrastructure (<c>AddOptions</c>, <c>AddLogging</c>) that a
/// generic host would normally provide, so options-bound services activate.
/// </summary>
public static class NotifliwyTestProviders
{
    /// <summary>
    /// Create a <see cref="ServiceCollection"/> with options + logging infrastructure
    /// and the default in-memory input registration (real production path).
    /// </summary>
    public static ServiceCollection CreateInMemoryCollection(
        Action<InMemoryExchangeOptions>? configureExchange = null)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddOptions();

        NotificationServerBuilder.CreateInstance(services)
            .AddInMemoryInput(configureExchange);

        return services;
    }

    /// <summary>
    /// Build a provider with options + logging infrastructure and the default
    /// in-memory input registration (real production path).
    /// </summary>
    public static ServiceProvider BuildInMemoryProvider(
        Action<InMemoryExchangeOptions>? configureExchange = null)
    {
        return CreateInMemoryCollection(configureExchange).BuildServiceProvider();
    }

    /// <summary>
    /// Create a <see cref="ServiceCollection"/> with options + logging infrastructure,
    /// ready for <c>AddNotifliwyServer</c> (end-to-end style tests).
    /// </summary>
    public static ServiceCollection CreateServerCollection()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddOptions();

        return services;
    }
}
