# Result Pattern - Notifliwy Project

## When to Use Result<T>

- **Expected failures**: Validation errors, not found, business rule violations
- **Don't use for**: Unexpected exceptions, infrastructure failures

Let unexpected exceptions propagate and use global exception handling.

## Basic Result Definition

```csharp
/// <summary>
/// Represents a successful or failed operation.
/// </summary>
public sealed record Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    private Result(bool isSuccess, string? error) => (IsSuccess, Error) = (isSuccess, error);

    public static Result Success() => new(isSuccess: true, error: null);
    public static Result Failure(string error) => new(isSuccess: false, error: error);
    public static Result<T> Success<T>(T value) => new(value: value, isSuccess: true, error: null);
    public static Result<T> Failure<T>(string error) => new(value: default, isSuccess: false, error: error);
}

/// <summary>
/// Represents a successful or failed operation with a value.
/// </summary>
public sealed record Result<T>(T? Value, bool IsSuccess, string? Error);
```

## Usage Examples

```csharp
// Method returning Result
public async Task<Result> ProcessNotificationAsync(
    Notification notification,
    CancellationToken cancellationToken = default)
{
    if (notification.Exporters.Count == 0)
    {
        return Result.Failure("No exporters configured");
    }

    await ExportAsync(notification, cancellationToken);
    return Result.Success();
}

// Method returning Result<T>
public async Task<Result<Notification>> ConvertAsync(
    Event input,
    CancellationToken cancellationToken = default)
{
    if (input is null)
    {
        return Result<Notification>.Failure("Input event cannot be null");
    }

    return Result<Notification>.Success(new Notification());
}

// Consuming Result
public async ValueTask ExportAsync(Event input, CancellationToken cancellationToken)
{
    var result = await mapper.ConvertAsync(input, cancellationToken);
    if (result.IsFailure)
    {
        logger.LogWarning("Conversion failed: {Error}", result.Error);
        return;
    }

    await exporter.ThrowAsync(result.Value, cancellationToken);
}
```

## Pipeline Integration

Notifliwy pipeline uses exceptions for flow control in conditions (return false to stop). Result pattern is for external-facing APIs and repository methods where you want explicit success/failure without exceptions.