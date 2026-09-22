namespace PerformancePatterns.Benchmarks;

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;

// Diagnoser-less counterpart of the main project's BenchmarkConfig: the linked benchmark classes bind to this
// type through their [Config(typeof(BenchmarkConfig))] attribute. NativeAOT rejects DisassemblyDiagnoser, so
// there is no Code Size column here; read code sizes from the main project's run.
public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddExporter(MarkdownExporter.GitHub);
        AddColumn(
            StatisticColumn.Mean,
            StatisticColumn.Min,
            StatisticColumn.Max,
            StatisticColumn.P90,
            StatisticColumn.Error,
            StatisticColumn.StdDev);
        AddDiagnoser(MemoryDiagnoser.Default);
    }
}
