using Notifliwy.Pipes.Interfaces;

namespace Notifliwy.Application.Demonstration;

public class InfinitySender(IExportPipe<IntChangeEvent> exportPipe) : IHostedService 
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Start sender");
        
        while (true)
        {
            Console.WriteLine("export event");
            await exportPipe.ExportAsync(new IntChangeEvent { Value = Random.Shared.Next(500, 2000) }, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}