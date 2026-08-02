namespace PerformancePatterns.Benchmarks.Lab;

using System.Text;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// TYP-06 study: comparing rebuild-every-time / dictionary cache / generic static for a string determined by type (a SQL fragment)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class StaticArtifactBenchmark
{
    private static readonly Dictionary<Type, string> Cache = [];

    [GlobalSetup]
    public void Setup()
    {
        Cache[typeof(OrderEntity)] = BuildSql(typeof(OrderEntity));
    }

    [Benchmark(Baseline = true)]
    public int BuildEveryCall() => BuildSql(typeof(OrderEntity)).Length;

    [Benchmark]
    public int DictionaryCache()
    {
        Cache.TryGetValue(typeof(OrderEntity), out var sql);
        return sql!.Length;
    }

    [Benchmark]
    public int StaticGenericField() => SqlInsert<OrderEntity>.Sql.Length;

    private static string BuildSql(Type type)
    {
        var builder = new StringBuilder();
        builder.Append("INSERT INTO ").Append(type.Name).Append(" (");
        foreach (var property in ColumnNames)
        {
            builder.Append(property).Append(", ");   // Always append the separator and trim it at the end
        }

        builder.Length -= 2;
        return builder.Append(") VALUES (@Id, @Name, @Amount, @CreatedAt)").ToString();
    }

    private static readonly string[] ColumnNames = ["Id", "Name", "Amount", "CreatedAt"];

    // Built once in the type initializer; after that it is only a static field read
    private static class SqlInsert<T>
    {
        public static readonly string Sql = BuildSql(typeof(T));
    }
}

// Entity types used as type arguments (for the benchmark)
public sealed class OrderEntity;
