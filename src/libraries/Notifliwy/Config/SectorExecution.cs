namespace Notifliwy.Config;

/// <summary>
/// Execution mode selected for a sector graph.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Auto"/> inspects node registrations at startup and picks the compiled
/// hot path when every node is compile-safe; otherwise it falls back to the scoped
/// path with a logged reason. <see cref="Compiled"/> forces the compiled path and
/// fails fast at startup with a captive-dependency error when any node requires
/// scoped services. <see cref="Scoped"/> always resolves every node from a fresh
/// DI scope per event.
/// </para>
/// <para>
/// A node is compile-safe when it is registered in DI as a singleton, or is
/// registered transient with only singleton-safe constructor dependencies, or is
/// not registered at all and has a public parameterless constructor (stateless
/// shape — one instance is created at startup and shared across all events).
/// Nodes on the compiled path must be thread-safe/stateless, exactly like
/// singleton-registered services.
/// </para>
/// </remarks>
public enum SectorExecution
{
    /// <summary>
    /// Picks the compiled hot path automatically when every node in the plan is
    /// compile-safe (singleton-registered or stateless/parameterless); otherwise
    /// falls back to the per-event scoped path. The chosen path and the fallback
    /// reason are logged at startup.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Forces the compiled hot path: node instances are resolved or constructed
    /// once at startup and the plan executes with direct invokes, without a
    /// per-event DI scope. Fails fast at startup with a
    /// <see cref="Exceptions.SectorCaptiveDependencyException"/> naming the sector
    /// and the offending node when any node requires scoped services or cannot be
    /// proven singleton-safe.
    /// </summary>
    Compiled = 1,

    /// <summary>
    /// Forces per-event DI scope execution: every node is resolved from a
    /// scope created for the processed event.
    /// </summary>
    Scoped = 2,
}
