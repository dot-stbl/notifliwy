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

namespace Notifliwy.Contexts;

/// <summary>
/// Base derived type of <see cref="INotificationSector{TEvent}"/>
/// </summary>
/// <inheritdoc />
public class NotificationSector<TNotification, TEvent>(
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationSector<TNotification, TEvent>>? logger) : INotificationSector<TEvent>
{
    /// <inheritdoc />
    public async ValueTask PassThroughAsync(
        TEvent inputEvent, 
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticActivity.NotifliwySource.StartSectorActivity<TNotification, TEvent>();

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope()
                .BlockBy<TNotification, TEvent>(out var sectorBlock);
            
            await sectorBlock.ProcessingAsync(inputEvent, cancellationToken);
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
                    delta: 1, 
                    tagList: DiagnosticSectorData<TNotification, TEvent>.TagsBy);
            });
        }
    }
}