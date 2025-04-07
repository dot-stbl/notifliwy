using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Models.Interfaces;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Conditions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Notifliwy.Connectors;
using Notifliwy.Dependency;

namespace Notifliwy.Benchmark;

[MemoryDiagnoser, ThreadingDiagnoser]
public class EventConditionBenchmarkRunner
{
    public IServiceCollection ServiceCollection { get; } = new ServiceCollection();
    
    [Params(1_000, 1_000_000)]
    public long CountEvent { get; set; }
     
    public EventConditionBenchmarkRunner()
    {
        ServiceCollection.AddLogging();
        
        ServiceCollection.AddNotifliwyServer(builder =>
        {
            builder.AddNotification<IntNotification, IntChangeEvent>(sectorBuilder =>
            {
                sectorBuilder
                    .AddMapper<IntChangeMapper>()
                    .AddCondition<IntChangeCondition>()
                    .AddExporters<IntChangeExporter>();
            });

            builder.AddInMemoryInput();
        });
    }
    
    [Benchmark]
    public async Task ExecuteBenchmark(CancellationToken cancellationToken)
    {
        await using var serviceProvider = ServiceCollection.BuildServiceProvider();

        await serviceProvider
            .GetRequiredService<NotificationConnectorService<IntChangeEvent>>()
            .StartAsync(cancellationToken);
    }
}

public class IntChangeEvent : IEvent
{
    public int Value { get; set; }
}

public class IntNotification : INotification
{
    public required string Message { get; set; }
}

public class IntChangeMapper : INotificationMapper<IntNotification, IntChangeEvent>
{
    public ValueTask<IntNotification> ConvertAsync(
        IntChangeEvent inputEvent, 
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(new IntNotification
        {
            Message = $"bad int {inputEvent.Value}"
        });
    }
}

public class IntChangeCondition : INotificationCondition<IntNotification, IntChangeEvent>
{
    public ValueTask<bool> AllowItAsync(
        IntChangeEvent inputEvent, 
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(inputEvent.Value > 1000);
    }
}

public class IntChangeExporter : INotificationExporter<IntNotification>
{
    public async ValueTask ThrowAsync(
        IntNotification notification, 
        CancellationToken cancellationToken = default)
    {
        await Console.Out.WriteLineAsync(JsonSerializer.Serialize(notification));
    }
}