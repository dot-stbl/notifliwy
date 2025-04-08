using MassTransit;
using Notifliwy.Models.Interfaces;
using Notifliwy.Pipes.Interfaces;

namespace Notifliwy.Provider.MassTransit.Kafka.Pipe;

/// <summary>
/// Base <c>Kafka</c> exporter from <see cref="IConsumer{TMessage}"/>
/// </summary>
/// <typeparam name="TEvent">assigned class event type</typeparam>
public class KafkaConsumerPipe<TEvent>(IExportPipe<TEvent> exportPipe) : IConsumer<TEvent> 
        where TEvent : class, IEvent
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<TEvent> context)
    {
        await exportPipe.ExportAsync(context.Message, context.CancellationToken);
    }
}