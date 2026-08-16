namespace Notifliwy.Config;

/// <summary>
/// Execution mode selected for a sector graph.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Auto"/> keeps the default per-event scoped execution until the
/// compiled hot path lands; <see cref="Scoped"/> always resolves every node from
/// a fresh DI scope per event. <see cref="Compiled"/> is reserved for the
/// source-generated compiled path and currently falls back to
/// <see cref="Scoped"/> behaviour until compiler support lands.
/// </para>
/// </remarks>
public enum SectorExecution
{
    /// <summary>
    /// Picks the best available execution strategy automatically. Until the
    /// compiled path ships this behaves exactly like <see cref="Scoped"/>.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Reserved for the compiled hot path (singleton/stateless graphs only,
    /// with a captive-dependency guard that fails fast on scoped nodes).
    /// Falls back to <see cref="Scoped"/> until compiler support lands.
    /// </summary>
    Compiled = 1,

    /// <summary>
    /// Forces per-event DI scope execution: every node is resolved from a
    /// scope created for the processed event.
    /// </summary>
    Scoped = 2,
}
