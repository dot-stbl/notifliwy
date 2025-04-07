using Notifliwy.Conditions.Interfaces;
using Notifliwy.Models.Interfaces;

namespace Notifliwy.Units;

public class GeneralArchitectTest
{
    public record TestNotification : INotification
    {
        public int AlertValue { get; set; }
    }

    public record TestChangeIntEvent : IEvent
    {
        public int Value { get; init; }
    }
    
    public class TestChangeIntCondition(int markValue) : INotificationCondition<TestNotification, TestChangeIntEvent>
    {
        public ValueTask<bool> AllowItAsync(
            TestChangeIntEvent inputEvent, 
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(inputEvent.Value >= markValue);
        }
    }
    
    [Theory]
    [InlineData(10_000)]
    public async Task Test1(int count)
    {
        using var tokenSource = new CancellationTokenSource();
        
        await foreach (var intEvent in InputEvent(count).WithCancellation(tokenSource.Token))
        {
            //await NotificationHandler.HandleAsync(intEvent, tokenSource.Token);
        }
    }

    protected static async IAsyncEnumerable<TestChangeIntEvent> InputEvent(int count)
    {
        while (--count > 0)
        {
            yield return new TestChangeIntEvent
            {
                Value = Random.Shared.Next(0, 100_000)
            };
        }
    }
}