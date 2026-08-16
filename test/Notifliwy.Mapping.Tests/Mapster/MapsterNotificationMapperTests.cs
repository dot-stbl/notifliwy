using System.Threading;
using System.Threading.Tasks;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Mapping.Mapster;
using Shouldly;
using Xunit;

namespace Notifliwy.Mapping.Tests.Mapster;

/// <summary>
/// Unit tests for <see cref="MapsterNotificationMapper{TNotification, TEvent}"/>:
/// a TypeAdapterConfig rule (explicit config, global settings or compiled delegate)
/// adapts into the Notifliwy mapper contract.
/// </summary>
public class MapsterNotificationMapperTests
{
    [Fact]
    public async Task ConvertAsyncWithExplicitConfigReturnsMappedNotification()
    {
        var config = new TypeAdapterConfig();
        config.NewConfig<DogBarkEvent, DogBarkNotification>();

        INotificationMapper<DogBarkNotification, DogBarkEvent> mapper =
            new MapsterNotificationMapper<DogBarkNotification, DogBarkEvent>(config);

        (await mapper.ConvertAsync(new DogBarkEvent { Loudness = 7 })).Loudness.ShouldBe(7);
    }

    [Fact]
    public async Task ConvertAsyncWithConfiguredRuleAppliesRuleTransform()
    {
        var config = new TypeAdapterConfig();
        config.NewConfig<DogBarkEvent, DogBarkNotification>()
            .Map(destination => destination.Loudness, source => source.Loudness * 10);

        INotificationMapper<DogBarkNotification, DogBarkEvent> mapper =
            new MapsterNotificationMapper<DogBarkNotification, DogBarkEvent>(config);

        (await mapper.ConvertAsync(new DogBarkEvent { Loudness = 3 })).Loudness.ShouldBe(30);
    }

    [Fact]
    public async Task ConvertAsyncWithGlobalSettingsReturnsMappedNotification()
    {
        INotificationMapper<DogBarkNotification, DogBarkEvent> mapper =
            new MapsterNotificationMapper<DogBarkNotification, DogBarkEvent>();

        (await mapper.ConvertAsync(new DogBarkEvent { Loudness = 11 })).Loudness.ShouldBe(11);
    }

    [Fact]
    public async Task ConvertAsyncWithCompiledDelegateReturnsMappedNotification()
    {
        INotificationMapper<DogBarkNotification, DogBarkEvent> mapper =
            new MapsterNotificationMapper<DogBarkNotification, DogBarkEvent>(
                static inputEvent => new DogBarkNotification { Loudness = inputEvent.Loudness * 2 });

        (await mapper.ConvertAsync(new DogBarkEvent { Loudness = 3 })).Loudness.ShouldBe(6);
    }

    [Fact]
    public void AddNotifliwyMapsterMappingResolvesNotificationMapper()
    {
        var services = new ServiceCollection()
            .AddNotifliwyMapsterMapping(configure: config => config.NewConfig<DogBarkEvent, DogBarkNotification>())
            .AddNotifliwyMapsterMapping<DogBarkNotification, DogBarkEvent>();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<INotificationMapper<DogBarkNotification, DogBarkEvent>>()
            .ShouldBeOfType<MapsterNotificationMapper<DogBarkNotification, DogBarkEvent>>();
    }
}
