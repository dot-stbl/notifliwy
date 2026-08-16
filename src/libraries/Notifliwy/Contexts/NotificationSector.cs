using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notifliwy.Contexts.Interfaces;
using Notifliwy.Diagnostic;
using Notifliwy.Diagnostic.Additions;
using Notifliwy.Extensions;
using Notifliwy.Extensions.Dependency;
using Notifliwy.Graph.Internals;

namespace Notifliwy.Contexts;

/// <summary>
/// Base derived type of <see cref="INotificationSector{TEvent}"/> executing the
/// sector graph plan: a fresh DI scope is created per event and every graph node
/// is resolved from that scope. The graph executor (and therefore the plan) is
/// resolved eagerly in the constructor, so an invalid graph fails at sector
/// resolution — connector startup for a hosted server.
/// </summary>
/// <inheritdoc />
public class NotificationSector<TNotification, TEvent>(
    IServiceProvider serviceProvider) : INotificationSector<TEvent>
{
    private readonly SectorGraphExecutor<TNotification, TEvent> executor =
        serviceProvider.GetRequiredService<SectorGraphExecutor<TNotification, TEvent>>();

    private readonly IServiceScopeFactory scopeFactory =
        serviceProvider.GetRequiredService<IServiceScopeFactory>();

    private readonly ILogger<NotificationSector<TNotification, TEvent>>? logger =
        serviceProvider.GetService<ILogger<NotificationSector<TNotification, TEvent>>>();

    /// <inheritdoc />
    public async ValueTask PassThroughAsync(
        TEvent inputEvent,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticActivity.NotifliwySource.StartSectorActivity<TNotification, TEvent>();

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();

            await executor.ExecuteScopeAsync(scope.ServiceProvider, inputEvent, cancellationToken);
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            activity.RecordException(exception);

            logger?.LogError(
                exception: exception,
                eventId: DiagnosticEventConstants.ErrorSectorEvent,
                message: "Notification sector failed with exception");
        }
        finally
        {
            activity.AddMeter(() =>
            {
                DiagnosticMeter.SectorProcessingCounter.Add(
                    1,
                    DiagnosticSectorData<TNotification, TEvent>.TagsBy);
            });
        }
    }
}
