using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notifliwy.Config;
using Notifliwy.Config.Interfaces;
using Notifliwy.Contexts.Interfaces;
using Notifliwy.Dependency;
using Notifliwy.Graph.Internals;
using Notifliwy.Graph.Interfaces;

namespace Notifliwy.Benchmark;

/// <summary>
/// Shared graph shape for the execution mode comparison: one condition, one mapper,
/// one transform, one exporter — identical nodes for both the compiled and the scoped sector.
/// </summary>
internal static class BenchmarkGraphShape
{
    /// <summary>
    /// Describe the comparison graph on the given builder
    /// </summary>
    public static void Describe(ISectorGraphBuilder<BenchmarkNotification, BenchmarkEvent> graph)
    {
        graph
            .When<AllowAllCondition>()
            .Map<BenchmarkMapper>()
            .Transform<BenchmarkTransform>()
            .Export<NoOpExporter>();
    }
}

/// <summary>
/// Sector configuration forcing the compiled hot path over the shared graph shape
/// </summary>
public class CompiledBenchmarkSector : INotificationSectorConfig<BenchmarkNotification, BenchmarkEvent>
{
    /// <inheritdoc />
    public SectorExecution Execution => SectorExecution.Compiled;

    /// <inheritdoc />
    public void Configure(ISectorGraphBuilder<BenchmarkNotification, BenchmarkEvent> graph)
    {
        BenchmarkGraphShape.Describe(graph);
    }
}

/// <summary>
/// Sector configuration forcing the per-event scoped path over the same shared graph shape
/// </summary>
public class ScopedBenchmarkSector : INotificationSectorConfig<BenchmarkNotification, BenchmarkEvent>
{
    /// <inheritdoc />
    public SectorExecution Execution => SectorExecution.Scoped;

    /// <inheritdoc />
    public void Configure(ISectorGraphBuilder<BenchmarkNotification, BenchmarkEvent> graph)
    {
        BenchmarkGraphShape.Describe(graph);
    }
}

/// <summary>
/// Builds the service providers backing the execution mode comparison
/// </summary>
internal static class ExecutionModeProviderFactory
{
    /// <summary>
    /// Build a provider with a single sector of <typeparamref name="TConfig"/> and verify the
    /// executor actually selected <paramref name="expectedMode"/> — a wrong mode would silently
    /// benchmark the same path twice.
    /// </summary>
    /// <typeparam name="TConfig">sector configuration class</typeparam>
    /// <param name="expectedMode">execution mode the executor must report</param>
    public static ServiceProvider Build<TConfig>(SectorExecutionMode expectedMode)
            where TConfig : class
    {
        var services = new ServiceCollection()
            .AddLogging();

        services.AddNotifliwyServer(serverBuilder => serverBuilder.AddSector<TConfig>());

        var provider = services.BuildServiceProvider();

        var executor = provider.GetRequiredService<SectorGraphExecutor<BenchmarkNotification, BenchmarkEvent>>();

        if (executor.Decision.Mode != expectedMode)
        {
            throw new InvalidOperationException(
                $"expected {expectedMode} execution for {typeof(TConfig).Name}, got {executor.Decision.Mode}");
        }

        return provider;
    }
}

/// <summary>
/// Compares the two sector execution paths over an identical graph: the compiled hot path
/// (node instances resolved once at startup, direct invokes, no per-event DI scope) against
/// the scoped path (fresh DI scope per event, every node resolved from it)
/// </summary>
[MemoryDiagnoser]
public class CompiledVsScopedBenchmarks
{
    private ServiceProvider compiledProvider = null!;
    private ServiceProvider scopedProvider = null!;
    private INotificationSector<BenchmarkEvent> compiledSector = null!;
    private INotificationSector<BenchmarkEvent> scopedSector = null!;
    private BenchmarkEvent inputEvent = null!;

    /// <summary>
    /// Build one provider per execution mode, each registering the same graph shape,
    /// and assert the executor picked the requested path for both.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        compiledProvider = ExecutionModeProviderFactory.Build<CompiledBenchmarkSector>(SectorExecutionMode.Compiled);
        compiledSector = compiledProvider.GetRequiredService<INotificationSector<BenchmarkEvent>>();

        scopedProvider = ExecutionModeProviderFactory.Build<ScopedBenchmarkSector>(SectorExecutionMode.Scoped);
        scopedSector = scopedProvider.GetRequiredService<INotificationSector<BenchmarkEvent>>();

        inputEvent = new BenchmarkEvent { Value = 42 };
    }

    /// <summary>
    /// Release both built service providers
    /// </summary>
    [GlobalCleanup]
    public async Task CleanupAsync()
    {
        await ((IAsyncDisposable)compiledProvider).DisposeAsync();
        await ((IAsyncDisposable)scopedProvider).DisposeAsync();
    }

    /// <summary>
    /// Pass a single event through the compiled hot path
    /// </summary>
    [Benchmark]
    public async Task CompiledPathAsync()
    {
        await compiledSector.PassThroughAsync(inputEvent);
    }

    /// <summary>
    /// Pass a single event through the per-event scoped path
    /// </summary>
    [Benchmark]
    public async Task ScopedPathAsync()
    {
        await scopedSector.PassThroughAsync(inputEvent);
    }
}
