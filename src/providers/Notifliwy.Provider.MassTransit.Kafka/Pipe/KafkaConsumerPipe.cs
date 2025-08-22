using MassTransit;
using Notifliwy.Connectors;
using Notifliwy.Contexts.Interfaces;

namespace Notifliwy.Provider.MassTransit.Kafka.Pipe;

/// <summary>
/// Base <c>Kafka</c> exporter from <see cref="IConsumer{TMessage}"/> without <see cref="NotificationConnector{TEvent}"/>
/// </summary>
/// <typeparam name="TEvent">assigned class event type</typeparam>
public class KafkaConsumerPipe<TEvent>(IEnumerable<INotificationSector<TEvent>> notificationSectors) : IConsumer<TEvent> 
    where TEvent : class
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<TEvent> context)
    {
        await Parallel.ForEachAsync(
            source: notificationSectors,
            body: async (sector, cancellationToken) =>
            {
                await sector.PassThroughAsync(context.Message, cancellationToken);
            });
    }
}