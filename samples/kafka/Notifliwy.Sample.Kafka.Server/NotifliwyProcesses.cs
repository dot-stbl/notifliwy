using System.Text.Json;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Extensions.System;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Transform.Interfaces;

namespace Notifliwy.Sample.Kafka.Server;

/// <inheritdoc />
public class CatMeowCondition : INotificationCondition<CatMeowNotification, CatMeowEvent>
{
    /// <inheritdoc />
    public ValueTask<bool> AllowItAsync(
        CatMeowEvent inputEvent,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(inputEvent.Name.Equals("Yuki"));
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

#region Pipeline

/// <inheritdoc />
public class ColorNotificationTransform : INotificationTransform<CatMeowNotification>
{
    /// <inheritdoc />
    public ValueTask<CatMeowNotification> TransformAsync(
        CatMeowNotification notification,
        CancellationToken cancellationToken = default)
    {
        notification.Color = $"{RandomExtensions.NextEnum<ConsoleColor>()}";

        return ValueTask.FromResult(notification);
    }
}

/// <inheritdoc />
public class ConstantColorNotificationTransform : INotificationTransform<CatMeowNotification>
{
    /// <inheritdoc />
    public ValueTask<CatMeowNotification> TransformAsync(
        CatMeowNotification notification,
        CancellationToken cancellationToken = default)
    {
        if (Random.Shared.Next(0, 100) <= 50)
        {
            return ValueTask.FromResult(notification);
        }

        notification.Color = "Gradient";
        notification.KittyMean = "MOW";

        return ValueTask.FromResult(notification);
    }
}

/// <inheritdoc />
public class CatNotificationConsoleExporter : INotificationExporter<CatMeowNotification>
{
    /// <inheritdoc />
    public async ValueTask ThrowAsync(
        CatMeowNotification notification,
        CancellationToken cancellationToken = default)
    {
        await Console.Out.WriteLineAsync(JsonSerializer.Serialize(notification));
    }
}

/// <inheritdoc />
public class CatNotificationDatabaseExporter(TempDbContext dbContext) : INotificationExporter<CatMeowNotification>
{
    /// <inheritdoc />
    public async ValueTask ThrowAsync(
        CatMeowNotification notification,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Notifications.AddAsync(
            notification,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

#endregion