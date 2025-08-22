using System.Net.Mime;
using Confluent.Kafka;
using MassTransit.KafkaIntegration.Serializers;

namespace Synaptix.MassTransit.Kafka.Protobuf;

/// <summary>
/// Protobuf <c>kafka</c> serialization
/// </summary>
public class ProtobufKafkaSerializerFactory : IKafkaSerializerFactory
{
    /// <summary>
    /// Protobuf-net content type
    /// </summary>
    public ContentType ContentType { get; } = new("application/x-protobuf-net");

    /// <summary>
    /// Assigned <see cref="Confluent.Kafka.IDeserializer{T}"/>
    /// </summary>
    public IDeserializer<T> GetDeserializer<T>() => new ProtobufMassTransitDeserializer<T>();

    /// <summary>
    /// Assigned <see cref="IAsyncSerializer{T}"/>, <see cref="ISerializer{T}"/>
    /// </summary>
    public IAsyncSerializer<T> GetSerializer<T>() => new ProtobufMassTransitSerializer<T>();
}