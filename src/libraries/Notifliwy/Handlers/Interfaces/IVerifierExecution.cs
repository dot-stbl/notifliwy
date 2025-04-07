using System;
using System.Threading.Tasks;

namespace Notifliwy.Handlers.Interfaces;

/// <summary>
/// Execution checker, it can handle all possible exceptions or add its own diagnostics besides those already provided.
/// </summary>
public interface IVerifierExecution
{
    /// <summary>
    /// A lambda is passed to call and capture the entire execution stack over the notification handler
    /// </summary>
    /// <remarks>it is not recommended to execute pending logic in it</remarks>
    /// <param name="handlerExecution"><see cref="INotificationHandler{TEvent}"/> logic</param>
    public ValueTask VerifyAsync(Func<ValueTask> handlerExecution);
}