using Notifliwy.Diagnostic;
using OpenTelemetry.Metrics;

namespace Notifliwy.OpenTelemetry.Instrumentation.Extensions;

/// <summary>
/// <c>Metric</c> <see cref="Instrumentation"/> extensions to <see cref="OpenTelemetry"/>
/// </summary>
public static class MetricBuilderExtensions
{
    /// <summary>
    /// Add metrics from <see cref="DiagnosticMeter"/>
    /// </summary>
    public static MeterProviderBuilder AddNotifliwyServerInstrumentation(
        this MeterProviderBuilder providerBuilder)
    {
        return providerBuilder.AddMeter(DiagnosticMeter.EventCount.Name); 
    }
}