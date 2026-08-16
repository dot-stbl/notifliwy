using System.ComponentModel.DataAnnotations;

namespace Notifliwy.Sample.Kafka;

public class CatMeowNotification
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string KittyMean { get; set; }

    public string? Color { get; set; }
}

public class CatMeowEvent
{
    public required string Name { get; init; }

    public required string KittyMean { get; init; } = "simple meow";
}
