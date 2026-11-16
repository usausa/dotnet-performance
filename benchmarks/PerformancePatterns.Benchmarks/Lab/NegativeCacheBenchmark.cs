namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// COL-08 study: caching the absence of a key
// Question: a resolver that caches its hits but not its misses pays the full miss path every time - the lock, the
// generic argument array that reading GenericTypeArguments copies, and a fresh empty array per call. Recording the
// empty result turns later calls into a lock-free hit, at the price of a heavier first call per key. What is the
// break-even, and does the lock contention disappear?
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class NegativeCacheBenchmark
{
    private static readonly Type[] UnregisteredServices =
    [
        typeof(IEnumerable<int>), typeof(IEnumerable<long>), typeof(IEnumerable<short>), typeof(IEnumerable<byte>),
        typeof(IEnumerable<sbyte>), typeof(IEnumerable<ushort>), typeof(IEnumerable<uint>), typeof(IEnumerable<ulong>),
        typeof(IEnumerable<float>), typeof(IEnumerable<double>), typeof(IEnumerable<decimal>), typeof(IEnumerable<char>),
        typeof(IEnumerable<bool>), typeof(IEnumerable<Guid>), typeof(IEnumerable<DateTime>), typeof(IEnumerable<TimeSpan>),
        typeof(IEnumerable<object>), typeof(IEnumerable<Version>), typeof(IEnumerable<Uri>), typeof(IEnumerable<string>),
    ];

    private MiniRegistry withoutNegativeCache = default!;

    private MiniRegistry withNegativeCache = default!;

    [GlobalSetup]
    public void Setup()
    {
        withoutNegativeCache = new MiniRegistry(cacheMisses: false);
        withNegativeCache = new MiniRegistry(cacheMisses: true);

        // Warm both resolvers so the measured call is the steady-state one
        _ = withoutNegativeCache.Resolve(typeof(IEnumerable<Uri>));
        _ = withNegativeCache.Resolve(typeof(IEnumerable<Uri>));
        _ = withoutNegativeCache.Resolve(typeof(IEnumerable<string>));
        _ = withNegativeCache.Resolve(typeof(IEnumerable<string>));
    }

    // ❌ The miss is recomputed under the lock on every call, and a new empty array is handed out each time
    [Benchmark(Baseline = true)]
    public int MissWithoutNegativeCache() => withoutNegativeCache.Resolve(typeof(IEnumerable<Uri>)).Length;

    // ✅ The empty result is recorded, so later calls take the lock-free hit path
    [Benchmark]
    public int MissWithNegativeCache() => withNegativeCache.Resolve(typeof(IEnumerable<Uri>)).Length;

    // Reference point: a key that is registered has always taken the hit path
    [Benchmark]
    public int RegisteredHit() => withNegativeCache.Resolve(typeof(IEnumerable<string>)).Length;

    // ⚠️ The price of the negative cache: the first call per key copies the snapshot dictionary
    [Benchmark]
    public int ColdMissesWithoutNegativeCache() => ResolveAllOnce(cacheMisses: false);

    [Benchmark]
    public int ColdMissesWithNegativeCache() => ResolveAllOnce(cacheMisses: true);

    public static void Verify()
    {
        var benchmark = new NegativeCacheBenchmark();
        benchmark.Setup();
        if ((benchmark.MissWithoutNegativeCache() != 0) ||
            (benchmark.MissWithNegativeCache() != 0) ||
            (benchmark.RegisteredHit() != 2) ||
            (benchmark.ColdMissesWithoutNegativeCache() != 2) ||
            (benchmark.ColdMissesWithNegativeCache() != 2))
        {
            throw new InvalidOperationException("Verify failed. NegativeCache");
        }

        // Both resolvers must return the same element type, and the cached one must return the same instance
        var withoutCache = new MiniRegistry(cacheMisses: false);
        var withCache = new MiniRegistry(cacheMisses: true);
        foreach (var serviceType in UnregisteredServices)
        {
            var plain = withoutCache.Resolve(serviceType);
            var cached = withCache.Resolve(serviceType);
            if ((plain.GetType() != cached.GetType()) || (plain.Length != cached.Length))
            {
                throw new InvalidOperationException($"Verify failed. NegativeCache shape service=[{serviceType}]");
            }

            if (!ReferenceEquals(cached, withCache.Resolve(serviceType)))
            {
                throw new InvalidOperationException($"Verify failed. NegativeCache identity service=[{serviceType}]");
            }

            // Without the negative cache an unregistered key hands out a new array every call
            if ((plain.Length == 0) && ReferenceEquals(plain, withoutCache.Resolve(serviceType)))
            {
                throw new InvalidOperationException($"Verify failed. NegativeCache miss identity service=[{serviceType}]");
            }
        }
    }

    private static int ResolveAllOnce(bool cacheMisses)
    {
        var registry = new MiniRegistry(cacheMisses);
        var total = 0;
        foreach (var serviceType in UnregisteredServices)
        {
            total += registry.Resolve(serviceType).Length;
        }

        return total;
    }
}

// A minimal snapshot-swapping resolver, modelled on a component container's IEnumerable<T> path
internal sealed class MiniRegistry(bool cacheMisses)
{
    private readonly Lock sync = new();

    private readonly Dictionary<Type, int> registrations = new()
    {
        [typeof(string)] = 2,
    };

    private volatile Dictionary<Type, Array> slots = [];

    public Array Resolve(Type serviceType)
    {
        var snapshot = slots;
        if (snapshot.TryGetValue(serviceType, out var cached))
        {
            return cached;
        }

        lock (sync)
        {
            // Look the snapshot up again inside the lock so concurrent first calls settle on one array
            if (slots.TryGetValue(serviceType, out var raced))
            {
                return raced;
            }

            var elementType = serviceType.GenericTypeArguments[0];
            var count = registrations.GetValueOrDefault(elementType);
            var array = Array.CreateInstance(elementType, count);
            if (cacheMisses || (count > 0))
            {
                var grown = new Dictionary<Type, Array>(slots) { [serviceType] = array };
                slots = grown;
            }

            return array;
        }
    }
}
