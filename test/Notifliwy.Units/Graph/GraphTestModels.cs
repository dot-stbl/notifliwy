using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notifliwy.Graph;
using Notifliwy.Graph.Interfaces;
using Notifliwy.Graph.Internals;

namespace Notifliwy.Units.Graph;

/// <summary>
/// Shared payload types for sector graph tests.
/// </summary>
public sealed class GraphEvent
{
    /// <summary>
    /// Simple payload value.
    /// </summary>
    public int Value { get; init; }
}

/// <summary>
/// Shared notification type for sector graph tests.
/// </summary>
public sealed class GraphNotification
{
    /// <summary>
    /// Mapped payload value.
    /// </summary>
    public int Value { get; set; }
}

/// <summary>
/// Captures ordered node invocations across DI-resolved node classes.
/// Safe for parallel branch captures.
/// </summary>
public sealed class Recorder
{
    private readonly object gate = new();
    private readonly List<string> calls = [];

    /// <summary>
    /// Record a single invocation.
    /// </summary>
    public void Record(string call)
    {
        lock (gate)
        {
            calls.Add(call);
        }
    }

    /// <summary>
    /// Snapshot of the ordered call log.
    /// </summary>
    public IReadOnlyList<string> Calls
    {
        get
        {
            lock (gate)
            {
                return calls.ToArray();
            }
        }
    }
}

/// <summary>
/// Builds a service provider around one configured sector graph and resolves its executor.
/// </summary>
internal static class GraphTestHost
{
    public static (ServiceProvider Provider, SectorGraphExecutor<TNotification, TEvent> Executor) Build<TNotification, TEvent>(
        Action<ISectorGraphBuilder<TNotification, TEvent>> configure,
        Action<ServiceCollection>? configureServices = null)
            where TNotification : class
    {
        var services = new ServiceCollection();
        services.AddLogging();
        configureServices?.Invoke(services);

        var graphBuilder = new SectorGraphBuilder<TNotification, TEvent>();
        configure(graphBuilder);
        graphBuilder.RegisterGraph(services);

        var provider = services.BuildServiceProvider();
        var executor = provider.GetRequiredService<SectorGraphExecutor<TNotification, TEvent>>();

        return (provider, executor);
    }
}

/// <summary>
/// Minimal capturing logger for asserting executor logging behaviour.
/// </summary>
internal sealed class SpyLogger<TCategory> : ILogger<TCategory>
{
    /// <summary>
    /// Captured log entries.
    /// </summary>
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, formatter(state, exception)));
    }
}
