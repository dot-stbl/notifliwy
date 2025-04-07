using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Notifliwy.Handlers.Interfaces;

namespace Notifliwy.Handlers.Verifiers;

/// <summary>
/// General error checker from <c>Notifliwy</c>, logs all running processes
/// </summary>
public class ExceptionVerifier(ILogger<ExceptionVerifier> logger) : IVerifierExecution
{
    /// <inheritdoc />
    public async ValueTask VerifyAsync(Func<ValueTask> handlerExecution)
    {
        try
        {
            await handlerExecution();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, message: "Unhandled error in event processing");
        }
    }
}