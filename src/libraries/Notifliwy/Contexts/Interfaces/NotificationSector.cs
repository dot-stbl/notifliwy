using System;
using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Contexts.Compilers;
using Notifliwy.Models.Interfaces;

namespace Notifliwy.Contexts.Interfaces;

/// <summary>
/// Base derived type of <see cref="INotificationSector{TNotification,TEvent}"/>
/// </summary>
/// <inheritdoc />
public class NotificationSector<TNotification, TEvent> : INotificationSector<TNotification, TEvent> 
        where TNotification : INotification 
        where TEvent : IEvent
{
    /// <inheritdoc />
    public Func<TEvent, CancellationToken, ValueTask<bool>> CompiledCondition { get; }

    /// <inheritdoc />
    public Func<TNotification, CancellationToken, ValueTask> CompiledExporter { get; }

    /// <inheritdoc />
    public Func<TEvent, CancellationToken, ValueTask<TNotification>> CompiledMapper { get; }

    /// <inheritdoc />
    public Func<TNotification, CancellationToken, ValueTask<TNotification>> CompiledPipeline { get; }
    
    /// <summary>
    /// Create new instance of <see cref="NotificationSector{TNotification,TEvent}"/>
    /// </summary>
    public NotificationSector(SectorRuntimeCompiler<TNotification, TEvent> sectorCompiler)
    {
        CompiledMapper = sectorCompiler.CompileMapper();
        CompiledPipeline = sectorCompiler.CompilePipeline();
        CompiledExporter = sectorCompiler.CompileExporters();
        CompiledCondition = sectorCompiler.CompileCondition();
    }
}