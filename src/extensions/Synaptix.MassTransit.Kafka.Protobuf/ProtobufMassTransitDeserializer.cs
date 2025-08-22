using System;
using System.IO;
using Confluent.Kafka;
using ProtoBuf;
using SerializationContext = Confluent.Kafka.SerializationContext;

namespace Synaptix.MassTransit.Kafka.Protobuf;

/// <summary>
/// <see cref="ProtoBuf.Serializer"/> from <c>proto-buf</c> link as main serializer
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