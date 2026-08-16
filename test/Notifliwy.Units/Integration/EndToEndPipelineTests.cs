using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Config.Interfaces;
using Notifliwy.Dependency;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Graph.Interfaces;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Transform.Interfaces;
using Notifliwy.Units.Helpers;
using Shouldly;
using Xunit;

namespace Notifliwy.Units.Integration;

/// <summary>
/// End-to-end integration tests for the Notifliwy graph pipeline:
/// in-memory input pipe → connector → sector graph → exporters.
/// </summary>
public class EndToEndPipelineTests
{
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

    private class FirstSinkExporter(List<TestNotification> notifications) : INotificationExporter<TestNotification>
    {
        public ValueTask ThrowAsync(TestNotification notification, CancellationToken cancellationToken = default)
        {
            notifications.Add(notification);
            return ValueTask.CompletedTask;
        }
    }

    private class SecondSinkExporter(List<TestNotification> notifications) : INotificationExporter<TestNotification>
    {
        public ValueTask ThrowAsync(TestNotification notification, CancellationToken cancellationToken = default)
        {
            notifications.Add(notification);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Config-class sector taking a constructor dependency (options-like marker).
    /// </summary>
    private class ConfigSector(MarkerOptions options) : INotificationSectorConfig<TestNotification, TestEvent>
    {
        public void Configure(ISectorGraphBuilder<TestNotification, TestEvent> graph)
        {
            graph
                .Map((inputEvent, cancellationToken) => ValueTask.FromResult(new TestNotification
                {
                    Value = inputEvent.Value * options.Multiplier
                }))
                .Transform<StatusTransform>()
                .Export<CollectionExporter>();
        }
    }

    private class MarkerOptions
    {
        public int Multiplier { get; init; } = 2;
    }

    private static async Task<ServiceProvider> StartConnectorAsync(ServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var hostedServices = serviceProvider.GetServices<IHostedService>();

        foreach (var hostedService in hostedServices)
        {
            if (hostedService is BackgroundService)
            {
                await hostedService.StartAsync(CancellationToken.None);
            }
        }

        await Task.Delay(100);

        return serviceProvider;
    }

    private static async Task StopConnectorAsync(ServiceProvider serviceProvider)
    {
        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
        {
            await hostedService.StopAsync(CancellationToken.None);
        }

        await serviceProvider.DisposeAsync();
    }

    [Fact]
    public async Task EndToEnd_EventFlowsThroughFullGraph()
    {
        // Arrange
        var exportedNotifications = new List<TestNotification>();
        var services = NotifliwyTestProviders.CreateServerCollection();
        services.AddSingleton(new CollectionExporter(exportedNotifications));

        services.AddNotifliwyServer(serverBuilder =>
        {
            serverBuilder.AddInMemoryInput();
            serverBuilder.AddSector<TestNotification, TestEvent>(graph => graph
                .Map<SimpleMapper>()
                .Transform<MultiplyTransform>()
                .Transform<StatusTransform>()
                .Export<CollectionExporter>());
        });

        var serviceProvider = await StartConnectorAsync(services);

        // Act
        await serviceProvider
            .GetRequiredService<Notifliwy.Pipes.Interfaces.IExportPipe<TestEvent>>()
            .ExportAsync(new TestEvent { Value = 5 });

        await Task.Delay(200);
        await StopConnectorAsync(serviceProvider);

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
        services.AddSingleton(new CollectionExporter(exportedNotifications));

        services.AddNotifliwyServer(serverBuilder =>
        {
            serverBuilder.AddInMemoryInput();
            serverBuilder.AddSector<TestNotification, TestEvent>(graph => graph
                .Map<SimpleMapper>()
                .Export<CollectionExporter>());
        });

        var serviceProvider = await StartConnectorAsync(services);
        var exportPipe = serviceProvider.GetRequiredService<Notifliwy.Pipes.Interfaces.IExportPipe<TestEvent>>();

        // Act
        await exportPipe.ExportAsync(new TestEvent { Value = 1 });
        await exportPipe.ExportAsync(new TestEvent { Value = 2 });
        await exportPipe.ExportAsync(new TestEvent { Value = 3 });

        await Task.Delay(200);
        await StopConnectorAsync(serviceProvider);

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
        services.AddSingleton(new CollectionExporter(exportedNotifications));

        services.AddNotifliwyServer(serverBuilder =>
        {
            serverBuilder.AddInMemoryInput();
            serverBuilder.AddSector<TestNotification, TestEvent>(graph => graph
                .When<EvenCondition>()
                .Map<SimpleMapper>()
                .Export<CollectionExporter>());
        });

        var serviceProvider = await StartConnectorAsync(services);
        var exportPipe = serviceProvider.GetRequiredService<Notifliwy.Pipes.Interfaces.IExportPipe<TestEvent>>();

        // Act - Send odd and even values
        await exportPipe.ExportAsync(new TestEvent { Value = 1 }); // Odd - should be filtered
        await exportPipe.ExportAsync(new TestEvent { Value = 2 }); // Even - should pass
        await exportPipe.ExportAsync(new TestEvent { Value = 3 }); // Odd - should be filtered
        await exportPipe.ExportAsync(new TestEvent { Value = 4 }); // Even - should pass

        await Task.Delay(200);
        await StopConnectorAsync(serviceProvider);

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
        services.AddSingleton(new FirstSinkExporter(exported1));
        services.AddSingleton(new SecondSinkExporter(exported2));

        services.AddNotifliwyServer(serverBuilder =>
        {
            serverBuilder.AddInMemoryInput();
            serverBuilder.AddSector<TestNotification, TestEvent>(graph => graph
                .Map<SimpleMapper>()
                .Export<FirstSinkExporter>()
                .Export<SecondSinkExporter>());
        });

        var serviceProvider = await StartConnectorAsync(services);

        // Act
        await serviceProvider
            .GetRequiredService<Notifliwy.Pipes.Interfaces.IExportPipe<TestEvent>>()
            .ExportAsync(new TestEvent { Value = 10 });

        await Task.Delay(200);
        await StopConnectorAsync(serviceProvider);

        // Assert
        exported1.Count.ShouldBe(1);
        exported2.Count.ShouldBe(1);
        exported1[0].Value.ShouldBe(20);
        exported2[0].Value.ShouldBe(20);
    }

    [Fact]
    public async Task EndToEnd_ConfigClassSector_DeliversNotificationsThroughConfiguredGraph()
    {
        // Arrange - config class sector with a constructor dependency honoured from DI
        var exportedNotifications = new List<TestNotification>();
        var services = NotifliwyTestProviders.CreateServerCollection();
        services.AddSingleton(new CollectionExporter(exportedNotifications));
        services.AddSingleton(new MarkerOptions { Multiplier = 3 });

        services.AddNotifliwyServer(serverBuilder =>
        {
            serverBuilder.AddInMemoryInput();
            serverBuilder.AddSector<ConfigSector>();
        });

        var serviceProvider = await StartConnectorAsync(services);

        // Act
        await serviceProvider
            .GetRequiredService<Notifliwy.Pipes.Interfaces.IExportPipe<TestEvent>>()
            .ExportAsync(new TestEvent { Value = 6 });

        await Task.Delay(200);
        await StopConnectorAsync(serviceProvider);

        // Assert - mapper lambda used the injected options (6 * 3), transform ran, exporter received it
        exportedNotifications.Count.ShouldBe(1);
        exportedNotifications[0].Value.ShouldBe(18);
        exportedNotifications[0].Status.ShouldBe("Processed");
    }
}
