namespace Notifliwy.Graph;

/// <summary>
/// Failure policy for a <c>Branch</c> fan-out.
/// </summary>
public enum BranchPolicy
{
    /// <summary>
    /// Default. The first fault rethrows after all branches have been observed
    /// (equivalent to awaiting <see cref="System.Threading.Tasks.Task.WhenAll"/>).
    /// </summary>
    FailFast = 0,

    /// <summary>
    /// Per-branch failures are logged and skipped; survivors continue and the
    /// following join receives only the surviving branch outputs.
    /// </summary>
    BestEffort = 1,
}
