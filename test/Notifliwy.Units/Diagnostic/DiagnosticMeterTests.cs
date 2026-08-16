using Notifliwy.Diagnostic;
using Shouldly;
using Xunit;

namespace Notifliwy.Units.Diagnostic;

/// <summary>
/// Unit tests for <see cref="DiagnosticMeter"/> subscription names
/// </summary>
public class DiagnosticMeterTests
{
    [Fact]
    public void MeterName_ShouldMatchNotifliwyServerMeterName()
    {
        // Arrange / Act / Assert
        DiagnosticMeter.MeterName.ShouldBe(DiagnosticMeter.NotifliwyServerMeter.Name);
    }

    [Fact]
    public void MeterName_ShouldBePinnedWireName()
    {
        // Arrange / Act
        // pinned wire name — documented workaround for GH #6 is AddMeter("Notifliwy.Server");
        // instrument names (notifliwy.server.event.count) must never be used for AddMeter

        // Assert
        DiagnosticMeter.MeterName.ShouldBe("Notifliwy.Server");
    }
}
