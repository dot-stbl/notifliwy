using ProtoBuf;
using Confluent.Kafka;
using System.Net.Mime;
using Notifliwy.Models.Interfaces;
using MassTransit.KafkaIntegration.Serializers;
using SerializationContext = Confluent.Kafka.SerializationContext;

namespace Notifliwy.Sample.Kafka;

[ProtoContract]
public class CatMeowNotification : INotification
{
    [ProtoMember(1, IsRequired = true)]
    public required string KittyMean { get; init; }
}


[ProtoContract]
public class CatMeowEvent : IEvent
{
    [ProtoMember(1, IsRequired = true)]
    public required string Name { get; init; }
    
    [ProtoMember(2, IsRequired = true)]
    public required string KittyMean { get; init; } = "simple meow";
}

/// <summary>
/// Protobuf <c>kafka</c> serialization
/// </summary>
public class ProtobufKafkaSerializerFactory : IKafkaSerializerFactory
{
    /// <summary>
    /// Protobuf-net content type
    /// </summary>
    public ContentType ContentType { get; } = new ("application/x-protobuf-net");
    
    /// <summary>
    /// Assigned <see cref="Confluent.Kafka.IDeserializer{T}"/>
    /// </summary>
    public IDeserializer<T> GetDeserializer<T>() => new ProtobufMassTransitDeserializer<T>();

    /// <summary>
    /// Assigned <see cref="IAsyncSerializer{T}"/>, <see cref="ISerializer{T}"/>
    /// </summary>
    public IAsyncSerializer<T> GetSerializer<T>() => new ProtobufMassTransitSerializer<T>();
}

/// <summary>
/// <see cref="Serializer"/> from <c>proto-buf</c> link as main serializer
/// </summary>
public class ProtobufMassTransitSerializer<T> : ISerializer<T>, IAsyncSerializer<T>
{
    /// <inheritdoc />
    public byte[] Serialize(T data, SerializationContext context)
    {
        if (data == null)
        {
            return [];
        }
        
        using var protoStream = new MemoryStream();
        Serializer.SerializeWithLengthPrefix(protoStream, data, PrefixStyle.Fixed32);
        return protoStream.ToArray();
    }

    /// <inheritdoc />
    public async Task<byte[]> SerializeAsync(T data, SerializationContext context)
    {
        return await Task.Run(function: () => Serialize(data, context));
    }
}

/// <summary>
/// <see cref="Serializer"/> from <c>proto-buf</c> link as main serializer
/// </summary>
/// <typeparam name="T"></typeparam>
public class ProtobufMassTransitDeserializer<T> : IDeserializer<T>
{
    /// <inheritdoc />
    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (data.IsEmpty && isNull)
            return default!;
        
        using var stream = new MemoryStream(buffer: data.ToArray());
        return Serializer.DeserializeWithLengthPrefix<T>(stream, PrefixStyle.Fixed32);
    }
}