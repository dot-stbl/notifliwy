using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Contexts.Interfaces;
using Notifliwy.Dependency;
using Notifliwy.Generated;
using Shouldly;

namespace Notifliwy.Generators.Tests;

/// <summary>
/// End-to-end tests for the source-generated sector registration: the generator
/// compiles into this assembly from <c>[assembly: NotifliwySectors]</c> and the
/// tests consume the real generated <c>AddNotifliwySectors()</c> extension.
/// </summary>
public class GeneratedSectorRegistrationTests
{
    [Fact]
    public void AddNotifliwySectors_RegistersEveryConfiguredSector()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act - the extension exists only if the generator produced it at compile time
        services.AddNotifliwyServer(serverBuilder => serverBuilder.AddNotifliwySectors());

        // Assert - both marked config classes are registered as sectors
        using var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetService<INotificationSector<AlphaEvent>>().ShouldNotBeNull();
        serviceProvider.GetService<INotificationSector<BetaEvent>>().ShouldNotBeNull();
    }

    [Fact]
    public async Task GeneratedSectors_ProcessEventsThroughTheirGraphs()
    {
        // Arrange
        GeneratedSectorSinks.AlphaExports.Clear();
        GeneratedSectorSinks.BetaExports.Clear();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNotifliwyServer(serverBuilder => serverBuilder.AddNotifliwySectors());

        await using var serviceProvider = services.BuildServiceProvider();

        // Act
        await serviceProvider
            .GetRequiredService<INotificationSector<AlphaEvent>>()
            .PassThroughAsync(new AlphaEvent { Value = 21 });

        await serviceProvider
            .GetRequiredService<INotificationSector<AlphaEvent>>()
            .PassThroughAsync(new AlphaEvent { Value = -1 });

        await serviceProvider
            .GetRequiredService<INotificationSector<BetaEvent>>()
            .PassThroughAsync(new BetaEvent { Text = "ready" });

        // Assert - alpha maps through the class mapper, the condition filters the negative event;
        // beta maps through the inline lambda
        GeneratedSectorSinks.AlphaExports.Count.ShouldBe(1);
        GeneratedSectorSinks.AlphaExports.Single().Value.ShouldBe(42);

        GeneratedSectorSinks.BetaExports.Count.ShouldBe(1);
        GeneratedSectorSinks.BetaExports.Single().Text.ShouldBe("READY");
    }
}
