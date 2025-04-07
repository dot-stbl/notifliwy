using System;
using System.Transactions;
using Notifliwy.Handlers.Interfaces;

namespace Notifliwy.Exceptions;

/// <summary>
/// Notification <see cref="INotificationHandler{TEvent}"/> execute exception 
/// </summary>
/// <param name="message">message details exception</param>
/// <param name="innerException">inner exception if him exist</param>
public class EventTransactionException(string message, Exception? innerException = null) 
    : TransactionException(message, innerException);