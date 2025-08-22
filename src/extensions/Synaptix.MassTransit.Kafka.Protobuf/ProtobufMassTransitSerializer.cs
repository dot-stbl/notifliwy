using System.IO;
using ProtoBuf;
using Confluent.Kafka;
using System.Threading.Tasks;
using SerializationContext = Confluent.Kafka.SerializationContext;

namespace Synaptix.MassTransit.Kafka.Protobuf;

/// <summary>
/// <see cref="ProtoBuf.Serializer"/> from <c>proto-buf</c> link as main serializer
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