using System.Diagnostics;
using System.Reflection;

namespace Notifliwy.Diagnostic;

/// <summary>
/// <see cref="Notifliwy"/> diagnostic static tool
/// </summary>
public class DiagnosticActivity
{
    /// <summary>
    /// This <see cref="Assembly"/>
    /// </summary>
    internal static Assembly ThisAssembly { get; } = typeof(DiagnosticActivity).Assembly;
 
    /// <summary>
    /// Current activity source <c>instrument name</c>
    /// </summary>
    internal static readonly string InstrumentName = ThisAssembly.FullName ?? nameof(Notifliwy);
    
    /// <summary>
    /// Main <see cref="ActivitySource"/> for <see cref="Notifliwy"/>
    /// </summary>
    internal static ActivitySource NotifliwySource { get; } = new (
        name: InstrumentName, 
        version: $"{ThisAssembly.GetName().Version}");
}