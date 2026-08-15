using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Steps.Interfaces;
using Shouldly;
using Xunit;

namespace Notifliwy.Units.Pipeline;

/// <summary>
/// Unit tests for pipeline components
/// </summary>
public class PipelineComponentsTests
{
    private class TestNotification
    {
        public int Value { get; set; }
    }

    private class TestEvent
    {
        public int Value { get; init; }
    }

    public class NotificationConditionTests
    {
        [Fact]
        public async Task AllowItAsync_ShouldReturnTrue_WhenConditionPasses()
        {
            // Arrange
            var condition = new AlwaysTrueCondition();
            var testEvent = new TestEvent { Value = 42 };

            // Act
            var result = await condition.AllowItAsync(testEvent);

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task AllowItAsync_ShouldReturnFalse_WhenConditionFails()
        {
            // Arrange
            var condition = new AlwaysFalseCondition();
            var testEvent = new TestEvent { Value = 42 };

            // Act
            var result = await condition.AllowItAsync(testEvent);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task AllowItAsync_ShouldRespectCancellationToken()
        {
            // Arrange
            var condition = new CancellableCondition();
            var testEvent = new TestEvent { Value = 42 };
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Should.ThrowAsync<OperationCanceledException>(async () =>
            {
                await condition.AllowItAsync(testEvent, cts.Token);
            });
        }

        private class AlwaysTrueCondition : INotificationCondition<TestNotification, TestEvent>
        {
            public ValueTask<bool> AllowItAsync(TestEvent inputEvent, CancellationToken cancellationToken = default)
            {
                return ValueTask.FromResult(true);
            }
        }

        private class AlwaysFalseCondition : INotificationCondition<TestNotification, TestEvent>
        {
            public ValueTask<bool> AllowItAsync(TestEvent inputEvent, CancellationToken cancellationToken = default)
            {
                return ValueTask.FromResult(false);
            }
        }

        private class CancellableCondition : INotificationCondition<TestNotification, TestEvent>
        {
            public ValueTask<bool> AllowItAsync(TestEvent inputEvent, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(true);
            }
        }
    }

    public class NotificationMapperTests
    {
        [Fact]
        public async Task ConvertAsync_ShouldConvertEventToNotification()
        {
            // Arrange
            var mapper = new SimpleMapper();
            var testEvent = new TestEvent { Value = 42 };

            // Act
            var result = await mapper.ConvertAsync(testEvent);

            // Assert
            result.ShouldNotBeNull();
            result.Value.ShouldBe(84); // value * 2
        }

        [Fact]
        public async Task ConvertAsync_ShouldRespectCancellationToken()
        {
            // Arrange
            var mapper = new CancellableMapper();
            var testEvent = new TestEvent { Value = 42 };
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Should.ThrowAsync<OperationCanceledException>(async () =>
            {
                await mapper.ConvertAsync(testEvent, cts.Token);
            });
        }

        private class SimpleMapper : INotificationMapper<TestNotification, TestEvent>
        {
            public ValueTask<TestNotification> ConvertAsync(TestEvent inputEvent, CancellationToken cancellationToken = default)
            {
                return ValueTask.FromResult(new TestNotification { Value = inputEvent.Value * 2 });
            }
        }

        private class CancellableMapper : INotificationMapper<TestNotification, TestEvent>
        {
            public ValueTask<TestNotification> ConvertAsync(TestEvent inputEvent, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(new TestNotification { Value = inputEvent.Value * 2 });
            }
        }
    }

    public class NotificationStepTests
    {
        [Fact]
        public async Task AggregateAsync_ShouldTransformNotification()
        {
            // Arrange
            var step = new MultiplyValueStep();
            var notification = new TestNotification { Value = 10 };

            // Act
            var result = await step.AggregateAsync(notification);

            // Assert
            result.ShouldNotBeNull();
            result.Value.ShouldBe(100); // value * 10
        }

        [Fact]
        public async Task AggregateAsync_ShouldRespectCancellationToken()
        {
            // Arrange
            var step = new CancellableStep();
            var notification = new TestNotification { Value = 10 };
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Should.ThrowAsync<OperationCanceledException>(async () =>
            {
                await step.AggregateAsync(notification, cts.Token);
            });
        }

        private class MultiplyValueStep : INotificationStep<TestNotification>
        {
            public ValueTask<TestNotification> AggregateAsync(TestNotification notification, CancellationToken cancellationToken = default)
            {
                notification.Value *= 10;
                return ValueTask.FromResult(notification);
            }
        }

        private class CancellableStep : INotificationStep<TestNotification>
        {
            public ValueTask<TestNotification> AggregateAsync(TestNotification notification, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(notification);
            }
        }
    }

    public class NotificationExporterTests
    {
        [Fact]
        public async Task ThrowAsync_ShouldExportNotification()
        {
            // Arrange
            var exportedNotifications = new List<TestNotification>();
            var exporter = new ListExporter(exportedNotifications);
            var notification = new TestNotification { Value = 42 };

            // Act
            await exporter.ThrowAsync(notification);

            // Assert
            exportedNotifications.Count.ShouldBe(1);
            exportedNotifications[0].Value.ShouldBe(42);
        }

        [Fact]
        public async Task ThrowAsync_ShouldRespectCancellationToken()
        {
            // Arrange
            var exporter = new CancellableExporter();
            var notification = new TestNotification { Value = 42 };
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Should.ThrowAsync<OperationCanceledException>(async () =>
            {
                await exporter.ThrowAsync(notification, cts.Token);
            });
        }

        [Fact]
        public async Task ThrowAsync_ShouldHandleMultipleExports()
        {
            // Arrange
            var exportedNotifications = new List<TestNotification>();
            var exporter = new ListExporter(exportedNotifications);
            var notifications = new[]
            {
                new TestNotification { Value = 1 },
                new TestNotification { Value = 2 },
                new TestNotification { Value = 3 }
            };

            // Act
            foreach (var notification in notifications)
            {
                await exporter.ThrowAsync(notification);
            }

            // Assert
            exportedNotifications.Count.ShouldBe(3);
            exportedNotifications[0].Value.ShouldBe(1);
            exportedNotifications[1].Value.ShouldBe(2);
            exportedNotifications[2].Value.ShouldBe(3);
        }

        private class ListExporter(List<TestNotification> exportedNotifications) : INotificationExporter<TestNotification>
        {
            public ValueTask ThrowAsync(TestNotification notification, CancellationToken cancellationToken = default)
            {
                exportedNotifications.Add(notification);
                return ValueTask.CompletedTask;
            }
        }

        private class CancellableExporter : INotificationExporter<TestNotification>
        {
            public ValueTask ThrowAsync(TestNotification notification, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.CompletedTask;
            }
        }
    }
}
