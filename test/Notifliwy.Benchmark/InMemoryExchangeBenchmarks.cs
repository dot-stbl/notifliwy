using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Notifliwy.Pipes.InMemory;
using Notifliwy.Pipes.Interfaces;

namespace Notifliwy.Benchmark;

/// <summary>
/// Measures a single write/read roundtrip over the in memory event exchange
///     (the transport used by the <c>in memory</c> input/export pipes)
/// </summary>
[MemoryDiagnoser]
public class InMemoryExchangeBenchmarks
{
    private InMemoryExportPipe<BenchmarkEvent> exportPipe = null!;
    private InMemoryInputPipe<BenchmarkEvent> inputPipe = null!;
    private BenchmarkEvent inputEvent = null!;

    /// <summary>
    /// Create exchange with default bounded channel and wrap it into input/export pipes
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var exchange = new InMemoryEventExchange<BenchmarkEvent>(exchangeOptions: null);

        exportPipe = new InMemoryExportPipe<BenchmarkEvent>(exchange);
        inputPipe = new InMemoryInputPipe<BenchmarkEvent>(exchange);
        inputEvent = new BenchmarkEvent { Value = 42 };
    }

    /// <summary>
    /// Export a single event and accept it back, leaving the channel empty
    /// </summary>
    [Benchmark]
    public async Task ExportAndAcceptSingleEventAsync()
    {
        await exportPipe.ExportAsync(inputEvent, CancellationToken.None);

        await foreach (var _ in inputPipe.AcceptAsync(CancellationToken.None))
        {
            break;
        }
    }
}
