using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Notifliwy.Models.Interfaces;
using Notifliwy.Pipes.InMemory.Interfaces;
using Notifliwy.Pipes.InMemory.Options;

namespace Notifliwy.Pipes.InMemory;

/// <summary>
/// <see cref="InMemoryEventExchange{TEvent}"/> constants value
/// </summary>
file class InMemoryChannelConstants
{
    /// <summary>
    /// Default <see cref="BoundedChannelOptions"/> capacity
    /// </summary>
    public const int DefaultChannelCapacity = 1_000_000;
}

/// <inheritdoc />
public class InMemoryEventExchange<TEvent> : IInMemoryEventExchange<TEvent> 
    where TEvent : IEvent
{
    /// <inheritdoc />
    public Channel<TEvent> EventExchange { get; }
    
    /// <summary>
    /// Default in memory exchange constructor
    ///     - setup internal exchange <see cref="EventExchange"/>
    /// </summary>
    /// <param name="exchangeOptions">exchange options</param>
    public InMemoryEventExchange(IOptions<InMemoryExchangeOptions>? exchangeOptions)
    {
        if (exchangeOptions?.Value is {} options)
        {
            switch (options.ChannelOptions)
            {
                case null:
                {
                    break;
                }
                case BoundedChannelOptions boundedOptions:
                {
                    EventExchange = Channel.CreateBounded<TEvent>(boundedOptions);
                    return;
                }
                case UnboundedChannelOptions unboundedOptions:
                {
                    EventExchange = Channel.CreateUnbounded<TEvent>(unboundedOptions);
                    return;
                }
            }
        }

        var channelOptions = new BoundedChannelOptions(
            capacity: InMemoryChannelConstants.DefaultChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        
        EventExchange = Channel.CreateBounded<TEvent>(channelOptions);
    }
}