namespace Notifliwy.Graph.Internals;

/// <summary>
/// Effective execution mode chosen for one sector at startup, after analyzing
/// node registrations. Test/inspection surface for the mode decision.
/// </summary>
/// <param name="Mode">effective mode the sector executes with</param>
/// <param name="Reasons">
///     human-readable reasons for the decision: empty for the compiled path,
///     the blockers that prevented compilation (or the forced-mode note) for scoped
/// </param>
internal sealed record SectorExecutionDecision(SectorExecutionMode Mode, string[] Reasons)
{
    /// <summary>
    /// Compiled path chosen — every node is compile-safe.
    /// </summary>
    public static SectorExecutionDecision ForCompiled()
    {
        return new SectorExecutionDecision(SectorExecutionMode.Compiled, []);
    }

    /// <summary>
    /// Scoped path chosen, with the reason(s) that blocked compilation.
    /// </summary>
    public static SectorExecutionDecision ForScoped(params string[] reasons)
    {
        return new SectorExecutionDecision(SectorExecutionMode.Scoped, reasons);
    }
}

/// <summary>
/// Effective execution strategy of a sector — the runtime counterpart of
/// <see cref="Config.SectorExecution"/> after the startup decision.
/// </summary>
internal enum SectorExecutionMode
{
    /// <summary>
    /// Compiled hot path: node instances resolved once at startup, direct invokes,
    /// no per-event DI scope.
    /// </summary>
    Compiled = 0,

    /// <summary>
    /// Per-event scoped path: every node resolved from a fresh DI scope.
    /// </summary>
    Scoped = 1,
}
