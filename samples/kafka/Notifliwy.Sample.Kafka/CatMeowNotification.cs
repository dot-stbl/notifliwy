using System.ComponentModel.DataAnnotations;
using ProtoBuf;
using Notifliwy.Models.Interfaces;

namespace Notifliwy.Sample.Kafka;

[ProtoContract]
public class CatMeowNotification : INotification
{
    [Key]
    [ProtoMember(tag: 3, IsRequired = true)]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [ProtoMember(tag: 1, IsRequired = true)]
    public required string KittyMean { get; set; }
    
    [ProtoMember(tag: 2, IsRequired = false)]
    public string? Color { get; set; }
}

[ProtoContract]
public class CatMeowEvent : IEvent
{
    [ProtoMember(1, IsRequired = true)]
    public required string Name { get; init; }
    
    [ProtoMember(2, IsRequired = true)]
    public required string KittyMean { get; init; } = "simple meow";
}