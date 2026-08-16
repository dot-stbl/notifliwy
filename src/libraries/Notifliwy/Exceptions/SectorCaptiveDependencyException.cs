using System;
using System.Collections.Generic;
using System.Linq;

namespace Notifliwy.Exceptions;

/// <summary>
/// Thrown when a sector requests <see cref="Config.SectorExecution.Compiled"/> but
/// its graph contains a node that cannot be executed on the compiled path —
/// a captive dependency (scoped registration) or an unregistered node with
/// constructor dependencies that cannot be proven singleton-safe.
/// </summary>
public class SectorCaptiveDependencyException(string sector, IReadOnlyList<string> blockers)
        : Exception(
            $"Sector {sector} requests SectorExecution.Compiled but the graph cannot be compiled: "
            + string.Join("; ", blockers.Select(blocker => $"{blocker}")));
