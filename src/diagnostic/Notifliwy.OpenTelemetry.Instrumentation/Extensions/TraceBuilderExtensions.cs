using Notifliwy.Diagnostic;
using OpenTelemetry.Trace;

namespace Notifliwy.OpenTelemetry.Instrumentation.Extensions;

/// <summary>
/// <c>Trace</c> <see cref="Instrumentation"/> extensions to <see cref="OpenTelemetry"/>
/// </summary>
public static class TraceBuilderExtensions
{
    /// <summary>
    /// Add traces from <see cref="DiagnosticActivity.NotifliwySource"/>
    /// </summary>
    public static TracerProviderBuilder AddNotifliwyServerInstrumentation(
        this TracerProviderBuilder providerBuilder)
    {
        return providerBuilder.AddSource(DiagnosticActivity.NotifliwySource.Name); 
    }
}