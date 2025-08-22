using Microsoft.EntityFrameworkCore;

namespace Notifliwy.Sample.Kafka.Server;

/// <inheritdoc />
public class TempDbContext(DbContextOptions<TempDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Demo table
    /// </summary>
    public DbSet<CatMeowNotification> Notifications { get; init; } = null!;
}