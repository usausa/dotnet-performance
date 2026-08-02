namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// DAT-01 study: column resolution strategies for row mapping (GetOrdinal per row / an ordinal-cache struct) and GetValue boxing
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class OrdinalResolveBenchmark
{
    private const int Rows = 1000;

    private FakeDataReader reader = default!;

    [GlobalSetup]
    public void Setup()
    {
        var ids = new int[Rows];
        var names = new string[Rows];
        var flags = new bool[Rows];
        for (var i = 0; i < Rows; i++)
        {
            ids[i] = i;
            names[i] = "Name" + (i % 10);
            flags[i] = (i & 1) == 0;
        }

        reader = new FakeDataReader(["Id", "Name", "Flag"], ids, names, flags);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Rows)]
    public long GetOrdinalPerRow()
    {
        reader.Reset();
        var total = 0L;
        while (reader.Read())
        {
            var id = reader.GetInt32(reader.GetOrdinal("Id"));
            var name = reader.GetString(reader.GetOrdinal("Name"));
            var flag = reader.GetBoolean(reader.GetOrdinal("Flag"));
            total += id + name.Length + (flag ? 1 : 0);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Rows)]
    public long CachedOrdinalsStruct()
    {
        reader.Reset();

        // Resolve once per reader; after that it is only a struct field read
        var ordinals = ResolveOrdinals(reader);
        var total = 0L;
        while (reader.Read())
        {
            total += MapRow(reader, in ordinals);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Rows)]
    public long CachedOrdinalsGetValueBoxing()
    {
        reader.Reset();
        var ordinals = ResolveOrdinals(reader);
        var total = 0L;
        while (reader.Read())
        {
            // GetValue plus a cast instead of the typed methods (value types get boxed)
            var id = (int)reader.GetValue(ordinals.Id);
            var name = (string)reader.GetValue(ordinals.Name);
            var flag = (bool)reader.GetValue(ordinals.Flag);
            total += id + name.Length + (flag ? 1 : 0);
        }

        return total;
    }

    private static long MapRow(FakeDataReader reader, in Ordinals ordinals)
    {
        var id = reader.GetInt32(ordinals.Id);
        var name = reader.GetString(ordinals.Name);
        var flag = reader.GetBoolean(ordinals.Flag);
        return id + name.Length + (flag ? 1 : 0);
    }

    private static Ordinals ResolveOrdinals(FakeDataReader reader)
    {
        int id = -1, name = -1, flag = -1;
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var column = reader.GetName(i);
            if (string.Equals(column, "Id", StringComparison.OrdinalIgnoreCase))
            {
                id = i;
            }
            else if (string.Equals(column, "Name", StringComparison.OrdinalIgnoreCase))
            {
                name = i;
            }
            else if (string.Equals(column, "Flag", StringComparison.OrdinalIgnoreCase))
            {
                flag = i;
            }
        }

        return new Ordinals(id, name, flag);
    }

    private readonly struct Ordinals(int id, int name, int flag)
    {
        public readonly int Id = id;

        public readonly int Name = name;

        public readonly int Flag = flag;
    }
}

// Minimal in-memory reader with a GetOrdinal (a dictionary lookup) equivalent to a real provider.
// Deriving from DbDataReader would pull in a chain of IDisposable ownership rules, so only the API shape needed for the measurement is reproduced.
// Being sealed, it excludes the virtual dispatch of a real provider (that is common to all three paths, so it is still valid for measuring the difference between resolution strategies)
internal sealed class FakeDataReader
{
    private readonly string[] names;

    private readonly Dictionary<string, int> lookup;

    private readonly int[] ids;

    private readonly string[] strings;

    private readonly bool[] flags;

    private int row = -1;

    public FakeDataReader(string[] names, int[] ids, string[] strings, bool[] flags)
    {
        this.names = names;
        this.ids = ids;
        this.strings = strings;
        this.flags = flags;
        lookup = Enumerable.Range(0, names.Length).ToDictionary(i => names[i], i => i, StringComparer.Ordinal);
    }

    public int FieldCount => names.Length;

    public void Reset() => row = -1;

    public bool Read() => ++row < ids.Length;

    public string GetName(int ordinal) => names[ordinal];

    public int GetOrdinal(string name)
        => lookup.TryGetValue(name, out var ordinal) ? ordinal : throw new KeyNotFoundException(name);

    public int GetInt32(int ordinal) => ordinal == 0 ? ids[row] : throw new InvalidCastException();

    public string GetString(int ordinal) => ordinal == 1 ? strings[row] : throw new InvalidCastException();

    public bool GetBoolean(int ordinal) => ordinal == 2 ? flags[row] : throw new InvalidCastException();

    public object GetValue(int ordinal) => ordinal switch
    {
        0 => ids[row],
        1 => strings[row],
        _ => flags[row],
    };
}
