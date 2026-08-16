using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Dependency;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Transform.Interfaces;
using Notifliwy.Units.Helpers;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Notifliwy.Units.Integration;

/// <summary>
/// End-to-end integration tests for Notifliwy pipeline
/// </summary>
public class EndToEndPipelineTests(ITestOutputHelper output)
{
    private ITestOutputHelper Output { get; } = output;

    private class TestNotification
    {
        public int Value { get; set; }
        public string? Status { get; set; }
    }

    private class TestEvent
    {
        public int Value { get; init; }
    }

    private class SimpleMapper : INotificationMapper<TestNotification, TestEvent>
    {
        public ValueTask<TestNotification> ConvertAsync(TestEvent inputEvent, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new TestNotification
            {
                Value = inputEvent.Value * 2,
                Status = "Mapped"
            });
        }
    }

    private class EvenCondition : INotificationCondition<TestNotification, TestEvent>
    {
        public ValueTask<bool> AllowItAsync(TestEvent inputEvent, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(inputEvent.Value % 2 == 0);
        }
    }

    private class MultiplyTransform : INotificationTransform<TestNotification>
    {
        public ValueTask<TestNotification> TransformAsync(TestNotification notification, CancellationToken cancellationToken = default)
        {
            notification.Value *= 3;
            return ValueTask.FromResult(notification);
        }
    }

    private class StatusTransform : INotificationTransform<TestNotification>
    {
        public ValueTask<TestNotification> TransformAsync(TestNotification notification, CancellationToken cancellationToken = default)
        {
            notification.Status = "Processed";
            return ValueTask.FromResult(notification);
        }
    }

    private class CollectionExporter(List<TestNotification> notifications) : INotificationExporter<TestNotification>
    {
        public ValueTask ThrowAsync(TestNotification notification, CancellationToken cancellationToken = default)
        {
            notifications.Add(notification);
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task EndToEnd_EventFlowsThroughFullPipeline()
    {
        // Arrange
        var exportedNotifications = new List<TestNotification>();
        var services = NotifliwyTestProviders.CreateServerCollection();
        services.AddSingleton(exportedNotifications);
        services.AddSingleton<CollectionExporter>();

        services.AddNotifliwyServer(serverBuilder =>
        {
            serverBuilder.AddInMemoryInput();
            serverBuilder.AddNotification<TestNotification, TestEvent>(sectorBuilder =>
            {
                sectorBuilder.AddMapper<SimpleMapper>();
                sectorBuilder.WithPipeline(pipelineBuilder =>
                {
                    pipelineBuilder.AddStep<MultiplyTransform>();
                    pipelineBuilder.AddStep<StatusTransform>();
                });
                sectorBuilder.AddExporter<CollectionExporter>();
            });
        });

        var serviceProvider = services.BuildServiceProvider();
        var exportPipe = serviceProvider.GetRequiredService<Notifliwy.Pipes.Interfaces.IExportPipe<TestEvent>>();
        var hostedServices = serviceProvider.GetServices<IHostedService>();

        // Start the connector
        var connectorTask = Task.Run(async () =>
        {
            foreach (var hostedService in hostedServices)
            {
                if (hostedService is Microsoft.Extensions.Hosting.BackgroundService)
                {
                    await hostedService.StartAsync(CancellationToken.None);
                }
            }
        });

        // Give time for connector to start
        await Task.Delay(100);

        // Act
        await exportPipe.ExportAsync(new TestEvent { Value = 5 });

        // Wait for processing
        await Task.Delay(200);

        // Stop connector
        foreach (var hostedService in hostedServices)
        {
            await hostedService.StopAsync(CancellationToken.None);
        }

        // Assert
        exportedNotifications.Count.ShouldBe(1);
        exportedNotifications[0].Value.ShouldBe(30); // (5 * 2) * 3
        exportedNotifications[0].Status.ShouldBe("Processed");
    }

    [Fact]
    public async Task EndToEnd_MultipleEventsAreProcessedCorrectly()
    {
        // Arrange
        var exportedNotifications = new List<TestNotification>();
        var services = NotifliwyTestProviders.CreateServerCollection();
        services.AddSingleton(exportedNotifications);
        services.AddSingleton<CollectionExporter>();

        services.AddNotifliwyServer(serverBuilder =>
        {
            serverBuilder.AddInMemoryInput();
            serverBuilder.AddNotification<TestNotification, TestEvent>(sectorBuilder =>
            {
                sectorBuilder.AddMapper<SimpleMapper>();
                sectorBuilder.AddExporter<CollectionExporter>();
            });
        });

        var serviceProvider = services.BuildServiceProvider();
        var exportPipe = serviceProvider.GetRequiredService<Notifliwy.Pipes.Interfaces.IExportPipe<TestEvent>>();
        var hostedServices = serviceProvider.GetServices<IHostedService>();

        // Start connector
        foreach (var hostedService in hostedServices)
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        await Task.Delay(100);

        // Act
        await exportPipe.ExportAsync(new TestEvent { Value = 1 });
        await exportPipe.ExportAsync(new TestEvent { Value = 2 });
        await exportPipe.ExportAsync(new TestEvent { Value = 3 });

        await Task.Delay(200);

        // Stop connector
        foreach (var hostedService in hostedServices)
        {
            await hostedService.StopAsync(CancellationToken.None);
        }

        // Assert
        exportedNotifications.Count.ShouldBe(3);
        exportedNotifications[0].Value.ShouldBe(2);
        exportedNotifications[1].Value.ShouldBe(4);
        exportedNotifications[2].Value.ShouldBe(6);
    }

    [Fact]
    public async Task EndToEnd_ConditionFiltersEvents()
    {
        // Arrange
        var exportedNotifications = new List<TestNotification>();
        var services = NotifliwyTestProviders.CreateServerCollection();
        services.AddSingleton(exportedNotifications);
        services.AddSingleton<CollectionExporter>();

        services.AddNotifliwyServer(serverBuilder =>
        {
            serverBuilder.AddInMemoryInput();
            serverBuilder.AddNotification<TestNotification, TestEvent>(sectorBuilder =>
            {
                sectorBuilder.AddCondition<EvenCondition>();
                sectorBuilder.AddMapper<SimpleMapper>();
                sectorBuilder.AddExporter<CollectionExporter>();
            });
        });

        var serviceProvider = services.BuildServiceProvider();
        var exportPipe = serviceProvider.GetRequiredService<Notifliwy.Pipes.Interfaces.IExportPipe<TestEvent>>();
        var hostedServices = serviceProvider.GetServices<IHostedService>();

        // Start connector
        foreach (var hostedService in hostedServices)
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        await Task.Delay(100);

        // Act - Send odd and even values
        await exportPipe.ExportAsync(new TestEvent { Value = 1 }); // Odd - should be filtered
        await exportPipe.ExportAsync(new TestEvent { Value = 2 }); // Even - should pass
        await exportPipe.ExportAsync(new TestEvent { Value = 3 }); // Odd - should be filtered
        await exportPipe.ExportAsync(new TestEvent { Value = 4 }); // Even - should pass

        await Task.Delay(200);

        // Stop connector
        foreach (var hostedService in hostedServices)
        {
            await hostedService.StopAsync(CancellationToken.None);
        }

        // Assert
        exportedNotifications.Count.ShouldBe(2);
        exportedNotifications[0].Value.ShouldBe(4);
        exportedNotifications[1].Value.ShouldBe(8);
    }

    [Fact]
    public async Task EndToEnd_MultipleExportersReceiveNotification()
    {
        // Arrange
        var exported1 = new List<TestNotification>();
        var exported2 = new List<TestNotification>();
        var services = NotifliwyTestProviders.CreateServerCollection();
        services.AddSingleton<INotificationExporter<TestNotification>>(new CollectionExporter(exported1));
        services.AddSingleton<INotificationExporter<TestNotification>>(new CollectionExporter(exported2));

        services.AddNotifliwyServer(serverBuilder =>
        {
            serverBuilder.AddInMemoryInput();
            serverBuilder.AddNotification<TestNotification, TestEvent>(sectorBuilder =>
            {
                sectorBuilder.AddMapper<SimpleMapper>();
            });
        });

        var serviceProvider = services.BuildServiceProvider();
        var exportPipe = serviceProvider.GetRequiredService<Notifliwy.Pipes.Interfaces.IExportPipe<TestEvent>>();
        var hostedServices = serviceProvider.GetServices<IHostedService>();

        // Start connector
        foreach (var hostedService in hostedServices)
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        await Task.Delay(100);

        // Act
        await exportPipe.ExportAsync(new TestEvent { Value = 10 });

        await Task.Delay(200);

        // Stop connector
        foreach (var hostedService in hostedServices)
        {
            await hostedService.StopAsync(CancellationToken.None);
        }

        // Assert
        exported1.Count.ShouldBe(1);
        exported2.Count.ShouldBe(1);
        exported1[0].Value.ShouldBe(20);
        exported2[0].Value.ShouldBe(20);
    }
}
