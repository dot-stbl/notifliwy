using System.Threading.Channels;
using Confluent.Kafka;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Connectors;
using Notifliwy.Pipes.InMemory;
using Notifliwy.Pipes.InMemory.Interfaces;
using Notifliwy.Pipes.InMemory.Options;
using Notifliwy.Pipes.Interfaces;
using Notifliwy.Provider.MassTransit.Kafka.Pipe;
using MicrosoftExtensions = Microsoft.Extensions.Options;

namespace Notifliwy.Provider.MassTransit.Kafka.Extensions;

/// <summary>
/// Extensions methods to configure the consumer as an <see cref="IExportPipe{TEvent}"/>
/// </summary>
public static class MassTransitKafkaExtensions
{
    /// <summary>
    /// Add <c>notifliwy</c> pipe <see cref="KafkaConsumerPipe{TEvent}"/> as <see cref="IConsumer{TMessage}"/>
    /// </summary>
    /// <param name="registrationConfigurator"><c>rider</c> registration configuration options</param>
    /// <typeparam name="TEvent">assigned event as consume message</typeparam>
    public static IConsumerRegistrationConfigurator<KafkaConsumerPipe<TEvent>> AddNotifliwyPipe<TEvent>(
        this IRiderRegistrationConfigurator registrationConfigurator)
            where TEvent : class
    {
        registrationConfigurator.AddSingleton(
            typeof(IInMemoryEventExchange<TEvent>),
            typeof(InMemoryEventExchange<TEvent>));

        registrationConfigurator.AddTransient(
            typeof(IExportPipe<TEvent>),
            typeof(InMemoryExportPipe<TEvent>));

        registrationConfigurator.AddTransient(
            typeof(IInputPipe<TEvent>),
            typeof(InMemoryInputPipe<TEvent>));

        registrationConfigurator.AddSingleton(
            MicrosoftExtensions.Options.Create(new InMemoryExchangeOptions
            {
                ChannelOptions = new BoundedChannelOptions(10_000)
                {
                    FullMode = BoundedChannelFullMode.Wait
                }
            }));

        registrationConfigurator.AddScoped<IConsumer<TEvent>, KafkaConsumerPipe<TEvent>>();

        return registrationConfigurator.AddConsumer<KafkaConsumerPipe<TEvent>>();
    }

    /// <summary>
    /// Add <c>notifliwy</c> pipe <see cref="KafkaConsumerPipe{TEvent}"/> as <see cref="IConsumer{TMessage}"/>
    /// </summary>
    /// <param name="endpointConfigurator"></param>
    /// <param name="registrationContext"><c>rider</c> registration context</param>
    /// <param name="withConnector">add consumer with integration <see cref="NotificationConnector{TEvent}"/></param>
    /// <typeparam name="TEvent">assigned event as consume message</typeparam>
    /// <typeparam name="TId">kafka id</typeparam>
    public static IKafkaTopicReceiveEndpointConfigurator<TId, TEvent> ConfigureNotifliwyPipe<TId, TEvent>(
        this IKafkaTopicReceiveEndpointConfigurator<TId, TEvent> endpointConfigurator,
        IRiderRegistrationContext registrationContext,
        bool withConnector = false)
            where TEvent : class
    {
        if (withConnector)
        {
            endpointConfigurator.ConfigureConsumer<KafkaConnectorConsumerPipe<TEvent>>(registrationContext);
        }
        else
        {
            endpointConfigurator.ConfigureConsumer<KafkaConsumerPipe<TEvent>>(registrationContext);
        }

        return endpointConfigurator;
    }

    /// <summary>
    /// Add <c>notifliwy</c> pipe <see cref="KafkaConsumerPipe{TEvent}"/> as <see cref="IConsumer{TMessage}"/>
    /// </summary>
    /// <param name="endpointConfigurator"></param>
    /// <param name="registrationContext"><c>rider</c> registration context</param>
    /// <param name="withConnector">add consumer with integration <see cref="NotificationConnector{TEvent}"/></param>
    /// <typeparam name="TEvent">assigned event as consume message</typeparam>
    public static IKafkaTopicReceiveEndpointConfigurator<Ignore, TEvent> ConfigureNotifliwyPipe<TEvent>(
        this IKafkaTopicReceiveEndpointConfigurator<Ignore, TEvent> endpointConfigurator,
        IRiderRegistrationContext registrationContext,
        bool withConnector = false)
            where TEvent : class
    {
        if (withConnector)
        {
            endpointConfigurator.ConfigureConsumer<KafkaConnectorConsumerPipe<TEvent>>(registrationContext);
        }
        else
        {
            endpointConfigurator.ConfigureConsumer<KafkaConsumerPipe<TEvent>>(registrationContext);
        }

        return endpointConfigurator;
    }
}