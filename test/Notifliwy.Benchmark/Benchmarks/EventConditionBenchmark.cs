using BenchmarkDotNet.Attributes;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Models.Interfaces;

namespace Notifliwy.Benchmark.Benchmarks;

internal class TestChangeEvent : IEvent
{
    public int CurrentValue { get; init; }
}

internal class TestChangeNotification : INotification;

internal class TestChangeEventCondition(int triggerValue) 
    : INotificationCondition<TestChangeNotification, TestChangeEvent>
{
    public ValueTask<bool> AllowItAsync(TestChangeEvent inputEvent, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(triggerValue >= inputEvent.CurrentValue);
    }
}

[MemoryDiagnoser]
internal sealed class EventConditionBenchmark
{
    public TestChangeEvent[] Events { get; } = 10.ByParam();
    
    [Benchmark(Baseline = true)]
    public async ValueTask<bool> AllowAll()
    {
        var bl = false;

        foreach (var changeEvent in Events)
        {
            bl = await new TestChangeEventCondition(triggerValue: 6).AllowItAsync(changeEvent);
        }

        return bl;
    }
}

internal static class EnumerableExtensions
{
    public static TestChangeEvent[] ByParam(this int count)
    {
        return Enumerable.Range(0, count).Select(i => new TestChangeEvent { CurrentValue = i }).ToArray();
    }
}