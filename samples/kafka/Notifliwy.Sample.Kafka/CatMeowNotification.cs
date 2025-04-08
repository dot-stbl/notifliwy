using ProtoBuf;
using Notifliwy.Models.Interfaces;

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