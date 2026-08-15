# Logging Patterns - Notifliwy Project

## Structured Logging

Always use structured logging with named parameters:

```csharp
// ✅ Correct - structured logging
logger.LogDebug(
    "Processing event {EventHash} / {EventType}",
    inputEvent?.GetHashCode(),
    DiagnosticEventData<TEvent>.EventSeparation);

logger.LogInformation("Notification {NotificationType} exported", typeof(TNotification).Name);

logger.LogError(
    exception,
    "Notification sector failed with exception");

// ✅ With multiple parameters
logger.LogWarning(
    "Retry attempt {AttemptNumber} for notification {NotificationType}",
    attempt, typeof(TNotification).Name);
```

## Anti-Patterns

```csharp
// ❌ Wrong - string interpolation
logger.LogInformation($"Processing event {inputEvent.Id}");

// ❌ Wrong - string concatenation
logger.LogInformation("Processing event " + inputEvent.Id);
```

## Log Levels

| Level | Usage |
|-------|-------|
| Debug | Detailed debugging info (event hash, type separation) |
| Information | General flow (notification exported, condition passed) |
| Warning | Unexpected but handled (retry attempts, skipped events) |
| Error | Failures that allow continuation (sector exceptions) |
| Critical | Severe errors (connector failure) |

## Notifliwy Specific Patterns

```csharp
// Activity tracing
using var activity = DiagnosticActivity.NotifliwySource.StartConnectorActivity<TEvent>();
activity?.SetStatus(ActivityStatusCode.Error);
activity.RecordException(exception);

// Meter metrics
DiagnosticMeter.InputCounter.Add(delta: 1, tagList: DiagnosticEventData<TEvent>.TagsBy);
```