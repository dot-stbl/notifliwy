using Notifliwy.Config;
using Notifliwy.Dependency;
using Notifliwy.Generated;
using Notifliwy.Pipes.Interfaces;
using Notifliwy.Sample.InMemory;

// marks this assembly for the Notifliwy source generator (ships inside the
// Notifliwy package): at compile time it emits NotifliwySectorsRegistration,
// which registers every INotificationSectorConfig<,> class below with zero
// runtime reflection
[assembly: NotifliwySectors]

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNotifliwyServer(serverBuilder =>
{
    serverBuilder.AddInMemoryInput();

    // generated from [assembly: NotifliwySectors] — equivalent to calling
    // serverBuilder.AddSector<TelemetrySector>() by hand
    serverBuilder.AddNotifliwySectors();
});

builder.Services.AddHostedService<PutterService>();

var application = builder.Build();

application.MapGet("/", () => "noy");

application.Run();

public class PutterService(IExportPipe<TelemetryEvent> exportPipe) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await exportPipe.ExportAsync(new TelemetryEvent(Random.Shared.Next()), stoppingToken);

            await Task.Delay(TimeSpan.FromMilliseconds(250), stoppingToken);
        }
    }
}
