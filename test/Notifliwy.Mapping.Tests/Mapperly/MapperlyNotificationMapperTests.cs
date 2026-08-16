using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Mapping.Mapperly;
using Shouldly;
using Xunit;

namespace Notifliwy.Mapping.Tests.Mapperly;

/// <summary>
/// Unit tests for <see cref="MapperlyNotificationMapper{TNotification, TEvent, TMapper}"/>:
/// a Mapperly-generated mapper adapts into the Notifliwy mapper contract.
/// </summary>
public class MapperlyNotificationMapperTests
{
    [Fact]
    public async Task ConvertAsyncReturnsMappedNotification()
    {
        INotificationMapper<CatMeowNotification, CatMeowEvent> mapper = new CatMeowNotificationMapper();

        var notification = await mapper.ConvertAsync(new CatMeowEvent { Volume = 42 });

        notification.Volume.ShouldBe(42);
    }

    [Fact]
    public async Task ConvertAsyncMapsEveryEvent()
    {
        INotificationMapper<CatMeowNotification, CatMeowEvent> mapper = new CatMeowNotificationMapper();

        (await mapper.ConvertAsync(new CatMeowEvent { Volume = 1 })).Volume.ShouldBe(1);
        (await mapper.ConvertAsync(new CatMeowEvent { Volume = -7 })).Volume.ShouldBe(-7);
    }

    [Fact]
    public void AddNotifliwyMapperlyMappingResolvesNotificationMapper()
    {
        var services = new ServiceCollection()
            .AddNotifliwyMapperlyMapping<CatMeowNotification, CatMeowEvent, CatMeowMapper>();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<INotificationMapper<CatMeowNotification, CatMeowEvent>>()
            .ShouldBeOfType<MapperlyNotificationMapper<CatMeowNotification, CatMeowEvent, CatMeowMapper>>();
    }

    [Fact]
    public async Task ResolvedNotificationMapperConvertsEvents()
    {
        var services = new ServiceCollection()
            .AddNotifliwyMapperlyMapping<CatMeowNotification, CatMeowEvent, CatMeowMapper>();

        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<INotificationMapper<CatMeowNotification, CatMeowEvent>>();

        (await mapper.ConvertAsync(new CatMeowEvent { Volume = 9 })).Volume.ShouldBe(9);
    }
}
