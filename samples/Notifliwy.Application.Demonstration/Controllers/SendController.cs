using Microsoft.AspNetCore.Mvc;
using Notifliwy.Pipes.Interfaces;

namespace Notifliwy.Application.Demonstration.Controllers;

[ApiController]
public class SendController : Controller
{
    [HttpGet("create")]
    public async Task<IActionResult> Create([FromServices] IExportPipe<IntChangeEvent> exportPipe)
    {
        var intChangeEvent = new IntChangeEvent { Value = Random.Shared.Next(500, 2000) };
        await exportPipe.ExportAsync(intChangeEvent, CancellationToken.None);
        return Created();
    }
    
    [HttpGet("create-more")]
    public Task<IActionResult> CreateMore(
        [FromQuery] long count,
        [FromServices] IExportPipe<IntChangeEvent> exportPipe)
    {
        _ = Task.Run(function: async () =>
        {
            while (count-- > 0)
            {
                await exportPipe.ExportAsync(new IntChangeEvent
                {
                    Value = Random.Shared.Next(1000, 2000)
                });
            }
        });
        
        return Task.FromResult<IActionResult>(Created());
    }
}