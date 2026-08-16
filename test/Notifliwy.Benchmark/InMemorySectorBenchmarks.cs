using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Contexts.Interfaces;
using Notifliwy.Dependency;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Transform.Interfaces;

namespace Notifliwy.Benchmark;

/// <summary>
/// Sample event used by all in memory benchmarks
/// </summary>
public sealed class BenchmarkEvent
{
    /// <summary>
    /// Simple payload value
    /// </summary>
    public int Value { get; init; }
}

/// <summary>
/// Sample notification produced by <see cref="BenchmarkMapper"/>
/// </summary>
public sealed class BenchmarkNotification
{
    /// <summary>
    /// Mapped payload value
    /// </summary>
    public int Value { get; set; }
}

/// <summary>
/// Single always-allow condition
/// </summary>
public sealed class AllowAllCondition : INotificationCondition<BenchmarkNotification, BenchmarkEvent>
{
    /// <inheritdoc />
    public ValueTask<bool> AllowItAsync(BenchmarkEvent inputEvent, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(true);
    }
}

/// <summary>
/// Mapper that doubles the payload value
/// </summary>
public sealed class BenchmarkMapper : INotificationMapper<BenchmarkNotification, BenchmarkEvent>
{
    /// <inheritdoc />
    public ValueTask<BenchmarkNotification> ConvertAsync(BenchmarkEvent inputEvent, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(new BenchmarkNotification { Value = inputEvent.Value * 2 });
    }
}

/// <summary>
/// Pipeline transform that passes the notification through untouched
/// </summary>
public sealed class BenchmarkTransform : INotificationTransform<BenchmarkNotification>
{
    /// <inheritdoc />
    public ValueTask<BenchmarkNotification> TransformAsync(BenchmarkNotification notification, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(notification);
    }
}

/// <summary>
/// Exporter that drops the notification
/// </summary>
public sealed class NoOpExporter : INotificationExporter<BenchmarkNotification>
{
    /// <inheritdoc />
    public ValueTask ThrowAsync(BenchmarkNotification notification, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Measures a single event pass through a notification sector:
/// DI scope creation, condition check, mapping, pipeline step and export
/// </summary>
[MemoryDiagnoser]
public class InMemorySectorBenchmarks
{
    private ServiceProvider serviceProvider = null!;
    private INotificationSector<BenchmarkEvent> sector = null!;
    private BenchmarkEvent inputEvent = null!;

    /// <summary>
    /// Build a server with one sector: single condition, mapper, transform and exporter
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection()
            .AddLogging();

        services.AddNotifliwyServer(serverBuilder =>
        {
            serverBuilder.AddSector<BenchmarkNotification, BenchmarkEvent>(graph => graph
                .When<AllowAllCondition>()
                .Map<BenchmarkMapper>()
                .Transform<BenchmarkTransform>()
                .Export<NoOpExporter>());
        });

        serviceProvider = services.BuildServiceProvider();
        sector = serviceProvider.GetRequiredService<INotificationSector<BenchmarkEvent>>();
        inputEvent = new BenchmarkEvent { Value = 42 };
    }

    /// <summary>
    /// Release the built service provider
    /// </summary>
    [GlobalCleanup]
    public async Task CleanupAsync()
    {
        await ((IAsyncDisposable)serviceProvider).DisposeAsync();
    }

    /// <summary>
    /// Pass a single event through the whole sector pipeline
    /// </summary>
    [Benchmark]
    public async Task PassThroughSectorAsync()
    {
        await sector.PassThroughAsync(inputEvent);
    }
}
