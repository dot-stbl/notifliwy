using Notifliwy.Conditions.Interfaces;
using Notifliwy.Mapper.Interfaces;

namespace Notifliwy.Sample.Kafka.Server;

/// <inheritdoc />
public class CatMeowCondition : INotificationCondition<CatMeowNotification, CatMeowEvent>
{
    /// <inheritdoc />
    public ValueTask<bool> AllowItAsync(
        CatMeowEvent inputEvent, 
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(inputEvent.KittyMean == "i'm strong kitty");
    }
}

/// <inheritdoc />
public class CatMeowMapper : INotificationMapper<CatMeowNotification, CatMeowEvent>
{
    /// <inheritdoc />
    public ValueTask<CatMeowNotification> ConvertAsync(
        CatMeowEvent inputEvent, 
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(new CatMeowNotification
        {
            KittyMean = inputEvent.KittyMean
        });
    }
}