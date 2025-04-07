using System;
using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Conditions.Interfaces;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Models.Interfaces;
using Notifliwy.Steps.Interfaces;

namespace Notifliwy.Contexts.Interfaces;

/// <summary>
/// Scoped <c>notification block</c>, contains assigned logic handler
///     by <typeparamref name="TNotification"/> and <typeparamref name="TEvent"/> 
/// </summary>
public interface INotificationSector<TNotification, in TEvent>
    where TNotification : INotification
    where TEvent : IEvent
{
    /// <summary>
    /// Compiled action of <see cref="INotificationExporter{TNotification}"/>
    /// </summary>
    public Func<TNotification, CancellationToken, ValueTask> CompiledExporter { get; }
    
    /// <summary>
    /// Compiled action of <see cref="INotificationCondition{TNotification,TEvent}"/>
    /// </summary>
    public Func<TEvent, CancellationToken, ValueTask<bool>> CompiledCondition { get; }
 
    /// <summary>
    /// Compiled action of <see cref="INotificationMapper{TNotification,TEvent}"/>
    /// </summary>
    public Func<TEvent, CancellationToken, ValueTask<TNotification>> CompiledMapper { get; }
    
    /// <summary>
    /// Compiled action invoke step pipeline <see cref="INotificationStep{TNotification}"/>
    /// </summary>
    public Func<TNotification, CancellationToken, ValueTask<TNotification>> CompiledPipeline { get; }
}