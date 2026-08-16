using System.Text.Json;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Dependency;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Pipes.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNotifliwyServer(serverBuilder =>
{
    serverBuilder.AddInMemoryInput();
    serverBuilder.AddSector<TestNotification, TestEvent>(graph => graph
            .When<TestCondition>()
            .Map<TestMapper>()
            .Export<ConsoleNotificationExporter>());
});

builder.Services.AddHostedService<PutterService>();

var application = builder.Build();

application.MapGet("/", () => "noy");

application.Run();

public class TestNotification
{
    public int MultiplyValue { get; set; }
}

public class TestEvent
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

public class ConsoleNotificationExporter : INotificationExporter<TestNotification>
{
    public ValueTask ThrowAsync(TestNotification notification, CancellationToken cancellationToken = default)
    {
        Console.WriteLine(JsonSerializer.Serialize(notification));
        return ValueTask.CompletedTask;
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
