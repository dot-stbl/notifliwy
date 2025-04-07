using System.Threading;
using System.Threading.Tasks;
using Notifliwy.Models.Interfaces;

namespace Notifliwy.Exporters.Interfaces;

/// <summary>
/// Custom <typeparamref name="TNotification"/> exporter
/// </summary>
/// <typeparam name="TNotification"></typeparam>
public interface INotificationExporter<in TNotification>
    where TNotification : INotification
{
    /// <summary>
    /// Export <typeparamref name="TNotification"/> by custom logic 
    /// </summary>
    /// <remarks>This method does not imply throwing errors ;)</remarks>
    public ValueTask ThrowAsync(TNotification notification, CancellationToken cancellationToken = default);
}