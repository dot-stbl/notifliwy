using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Notifliwy.Benchmark;

/// <summary>
/// Shared BenchmarkDotNet configuration factory
/// </summary>
public static class BenchmarkConfig
{
    /// <summary>
    /// Allows running from a default (Debug) build, where the referenced <c>Notifliwy</c>
    /// assembly is not optimized - the benchmark assembly itself is always optimized
    ///     by <c>Optimize=true</c>. Use <c>-c Release</c> for the most precise results
    /// </summary>
    public static IConfig Create()
    {
        return ManualConfig
            .Create(DefaultConfig.Instance)
            .WithOptions(ConfigOptions.DisableOptimizationsValidator);
    }
}

/// <summary>
/// Entry point for the <c>Notifliwy</c> benchmark suite
/// </summary>
public static class Program
{
    /// <summary>
    /// Run all benchmarks, or filter them by passed arguments
    ///     (e.g. <c>dotnet run --project test/Notifliwy.Benchmark -- --filter *InMemory*</c>)
    /// </summary>
    /// <param name="args">BenchmarkDotNet console arguments</param>
    public static void Main(string[] args)
    {
        BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args, BenchmarkConfig.Create());
    }
}
