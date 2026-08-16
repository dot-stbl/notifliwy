using System;
using System.Collections.Generic;
using System.Linq;

namespace Notifliwy.Exceptions;

/// <summary>
/// Exception thrown when a sector graph violates the structural rules
/// (single <c>Map</c> before other nodes, <c>Join</c> after <c>Branch</c>,
/// branch termination, acyclicity) at plan build time.
/// </summary>
public class SectorGraphValidationException(string sector, IReadOnlyList<string> violations)
        : Exception(
            $"Invalid sector graph for {sector}: {string.Join("; ", violations)}");
