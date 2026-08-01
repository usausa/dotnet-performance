namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// 検証キュー⑤: P/Invoke 高速化(Windows 専用ベンチマーク)
// 問い: [LibraryImport](ソース生成マーシャリング)と [SuppressGCTransition]
// (短時間ネイティブ呼び出しの GC 遷移省略)はどれだけ効くか。
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

#pragma warning disable SYSLIB1054 // DllImport と LibraryImport の比較測定のため意図的に旧 API を使用
    [DllImport("kernel32.dll", EntryPoint = "GetTickCount64", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern ulong GetTickCount64Dll();
#pragma warning restore SYSLIB1054

    [LibraryImport("kernel32.dll", EntryPoint = "GetTickCount64")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial ulong GetTickCount64Lib();

    // 極短時間(数百 ns 未満)・ブロックしない・コールバックしない呼び出し専用
    [LibraryImport("kernel32.dll", EntryPoint = "GetTickCount64")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [SuppressGCTransition]
    private static partial ulong GetTickCount64LibFast();
}
