namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// Study queue 5: speeding up P/Invoke (Windows-only benchmark)
// Question: how much do [LibraryImport] (source-generated marshalling) and [SuppressGCTransition]
// (skipping the GC transition for short native calls) actually help?
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public partial class PInvokeBenchmark
{
    private const int N = 100;

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public ulong DllImportCall()
    {
        var total = 0UL;
        for (var i = 0; i < N; i++)
        {
            total += GetTickCount64Dll();
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public ulong LibraryImportCall()
    {
        var total = 0UL;
        for (var i = 0; i < N; i++)
        {
            total += GetTickCount64Lib();
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public ulong LibraryImportSuppressGC()
    {
        var total = 0UL;
        for (var i = 0; i < N; i++)
        {
            total += GetTickCount64LibFast();
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public ulong ManagedTickCount64()
    {
        var total = 0UL;
        for (var i = 0; i < N; i++)
        {
            total += (ulong)Environment.TickCount64;
        }

        return total;
    }

#pragma warning disable SYSLIB1054 // The old API is used deliberately in order to measure DllImport against LibraryImport
    [DllImport("kernel32.dll", EntryPoint = "GetTickCount64", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern ulong GetTickCount64Dll();
#pragma warning restore SYSLIB1054

    [LibraryImport("kernel32.dll", EntryPoint = "GetTickCount64")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial ulong GetTickCount64Lib();

    // Only for calls that are extremely short (under a few hundred ns), never block and never call back
    [LibraryImport("kernel32.dll", EntryPoint = "GetTickCount64")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [SuppressGCTransition]
    private static partial ulong GetTickCount64LibFast();
}
