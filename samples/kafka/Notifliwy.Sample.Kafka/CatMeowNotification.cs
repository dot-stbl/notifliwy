using System.ComponentModel.DataAnnotations;
using ProtoBuf;

namespace Notifliwy.Sample.Kafka;

[ProtoContract]
public class CatMeowNotification
{
    [Key]
    [ProtoMember(3, IsRequired = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [ProtoMember(1, IsRequired = true)]
    public required string KittyMean { get; set; }

    [ProtoMember(2, IsRequired = false)]
    public string? Color { get; set; }
}

[ProtoContract]
public class CatMeowEvent
{
    [ProtoMember(1, IsRequired = true)]
    public required string Name { get; init; }

    [ProtoMember(2, IsRequired = true)]
    public required string KittyMean { get; init; } = "simple meow";
}