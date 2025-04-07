using Notifliwy.Application.Demonstration;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Dependency;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Models.Interfaces;
using Notifliwy.OpenTelemetry.Instrumentation.Extensions;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resourceBuilder =>
    {
        resourceBuilder
            .AddService("notifliwy.demo")
            .AddTelemetrySdk()
            .AddEnvironmentVariableDetector();
    })
    .WithTracing(providerBuilder =>
    {
        providerBuilder.AddNotifliwyServerInstrumentation();

        providerBuilder.AddOtlpExporter(options =>
        {
            options.Protocol = OtlpExportProtocol.Grpc;
            options.Endpoint = new Uri("http://localhost:4317");
        });
    });

builder.Services.AddNotifliwyServer(serverBuilder =>
{
    serverBuilder.AddNotification<IntNotification, IntChangeEvent>(sectorBuilder =>
    {
        sectorBuilder
            .AddMapper<IntChangeMapper>()
            .AddCondition<IntChangeCondition>()
            .AddExporters<IntNotificationExporter>();
    });
    
    serverBuilder.AddInMemoryInput();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

namespace Notifliwy.Application.Demonstration
{
    public class IntNotification : INotification
    {
        public required string Message { get; set; }
    }
    
    public class IntChangeEvent : IEvent
    {
        public int Value { get; set; }
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
    
    public class IntNotificationExporter : INotificationExporter<IntNotification>
    {
        public ValueTask ThrowAsync(
            IntNotification notification, 
            CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}