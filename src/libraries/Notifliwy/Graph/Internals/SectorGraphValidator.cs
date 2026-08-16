using System;
using System.Collections.Generic;
using Notifliwy.Exceptions;

namespace Notifliwy.Graph.Internals;

/// <summary>
/// Structural validation of a recorded sector graph, run at plan build time
/// (startup). The graph is acyclic by construction — the builder registers a
/// linear main path and branch sub-graphs recurse into the same shape, so a
/// cycle cannot be expressed; everything else is validated here.
/// </summary>
internal static class SectorGraphValidator
{
    /// <summary>
    /// Walk the ordered registrations of one builder and collect every structural
    /// violation: <c>Map</c> presence and uniqueness, node ordering, <c>Join</c>
    /// placement after <c>Branch</c>, and branch termination.
    /// </summary>
    /// <param name="registrations">ordered registration list of one scope</param>
    /// <param name="branchScope">
    ///     <see langword="true"/> when validating a branch sub-graph, where <c>When</c> and <c>Map</c> are not allowed
    /// </param>
    /// <param name="violations">collector for found violations</param>
    public static void CollectViolations<TNotification, TEvent>(
        IReadOnlyList<GraphRegistration> registrations,
        bool branchScope,
        List<string> violations)
    {
        var mapRegistrations = 0;
        var mapSeen = false;
        var branchPending = false;

        foreach (var registration in registrations)
        {
            switch (registration)
            {
                case GraphWhenRegistration:
                    if (branchScope)
                    {
                        violations.Add("When is not allowed inside a branch sub-graph");
                    }
                    else if (mapSeen)
                    {
                        violations.Add("When must be registered before Map");
                    }

                    break;

                case GraphMapRegistration<TNotification, TEvent>:
                    mapRegistrations++;
                    mapSeen = true;

                    if (branchScope)
                    {
                        violations.Add("Map is not allowed inside a branch sub-graph");
                    }

                    break;

                case GraphNodeDefinition<TNotification, TEvent> node:
                    // branch sub-plans start from the mapped notification — no Map required there
                    if (!branchScope && !mapSeen)
                    {
                        violations.Add($"Map must be registered before {NodeName(node)}");
                    }

                    if (node is GraphJoinDefinition<TNotification, TEvent>)
                    {
                        if (!branchPending)
                        {
                            violations.Add("Join is only valid after a Branch node");
                        }
                        else
                        {
                            branchPending = false;
                        }
                    }

                    if (node is GraphBranchRegistration<TNotification, TEvent> branch)
                    {
                        branchPending = true;
                        CollectBranchViolations(branch, violations);
                    }

                    break;
            }
        }

        if (!branchScope && mapRegistrations == 0)
        {
            violations.Add("Map node is required exactly once: none registered");
        }
        else if (mapRegistrations > 1)
        {
            violations.Add($"Map node is required exactly once: registered {mapRegistrations} times");
        }
    }

    private static void CollectBranchViolations<TNotification, TEvent>(
        GraphBranchRegistration<TNotification, TEvent> branch,
        List<string> violations)
    {
        if (branch.BranchBuilders.Length == 0)
        {
            violations.Add("Branch requires at least one branch sub-graph");
            return;
        }

        foreach (var branchBuilder in branch.BranchBuilders)
        {
            CollectViolations<TNotification, TEvent>(
                branchBuilder.Registrations,
                branchScope: true,
                violations);

            var hasExport = false;

            foreach (var registration in branchBuilder.Registrations)
            {
                if (registration is GraphExportDefinition<TNotification, TEvent>)
                {
                    hasExport = true;
                    break;
                }
            }

            if (!hasExport)
            {
                violations.Add("Branch sub-graph must contain at least one Export node (dead-end branch)");
            }
        }
    }

    private static string NodeName<TNotification, TEvent>(
        GraphNodeDefinition<TNotification, TEvent> node)
    {
        return node switch
        {
            GraphTransformDefinition<TNotification, TEvent> => "Transform",
            GraphExportDefinition<TNotification, TEvent> => "Export",
            GraphBranchRegistration<TNotification, TEvent> => "Branch",
            GraphJoinDefinition<TNotification, TEvent> => "Join",
            GraphCustomDefinition<TNotification, TEvent> => "Custom",
            _ => node.GetType().Name
        };
    }
}
