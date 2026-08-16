using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Notifliwy.Config.Internals;

/// <summary>
/// Marker singleton registered by <see cref="Builders.NotificationServerBuilder.AddSectorsFromAssembly"/>.
/// Constructing it (once per container, resolved during sector executor startup)
/// logs the reflection-fallback warning.
/// </summary>
internal sealed class SectorAssemblyScanNotice
{
    /// <summary>
    /// Warning message emitted when sectors were discovered by reflection instead
    /// of the source-generated registration.
    /// </summary>
    public const string WarningMessage =
        "AddSectorsFromAssembly uses the reflection fallback; prefer [NotifliwySectors] source-gen registration";
}

/// <summary>
/// Registration helper for the assembly-scan warning notice.
/// </summary>
internal static class SectorAssemblyScanNoticeRegistration
{
    /// <summary>
    /// Register the one-shot warning: the singleton factory logs when the notice
    /// is first resolved at executor startup.
    /// </summary>
    public static void AddSectorAssemblyScanNotice(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<SectorAssemblyScanNotice>(serviceProvider =>
        {
            serviceProvider
                .GetService<ILogger<SectorAssemblyScanNotice>>()?
                .LogWarning("{Message}", SectorAssemblyScanNotice.WarningMessage);

            return new SectorAssemblyScanNotice();
        });
    }
}
