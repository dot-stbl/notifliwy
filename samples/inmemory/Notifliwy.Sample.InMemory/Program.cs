using Notifliwy.Conditions.Interfaces;
using Notifliwy.Dependency;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Models.Interfaces;
using Notifliwy.Pipes.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNotifliwyServer(serverBuilder =>
{
    serverBuilder.AddInMemoryInput();
    serverBuilder.AddNotification<TestNotification, TestEvent>(sectorBuilder =>
    {
        sectorBuilder
            .AddMapper<TestMapper>()
            .AddCondition<TestCondition>();
    });
});

builder.Services.AddHostedService<PutterService>();

var application = builder.Build();

application.MapGet("/", () => "noy");

application.Run();

public class TestNotification : INotification
{
    public int MultiplyValue { get; set; }
}

public class TestEvent : IEvent
{
    public int InputValue { get; init; }
}

public class TestMapper : INotificationMapper<TestNotification, TestEvent>
{
    public ValueTask<TestNotification> ConvertAsync(TestEvent inputEvent, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(new TestNotification { MultiplyValue = inputEvent.InputValue });
    }
}

public class TestCondition : INotificationCondition<TestNotification, TestEvent>
{
    public ValueTask<bool> AllowItAsync(
        TestEvent inputEvent, 
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(inputEvent.InputValue % 5 == 0);
    }
}

public class PutterService(IExportPipe<TestEvent> exportPipe) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await exportPipe.ExportAsync(new TestEvent { InputValue = Random.Shared.Next() }, stoppingToken);
        }
    }
}