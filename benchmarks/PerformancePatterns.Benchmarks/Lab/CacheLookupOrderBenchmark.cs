namespace PerformancePatterns.Benchmarks.Lab;

using System.Collections.Concurrent;
using System.Reflection;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// TYP-08 study: what runs before the cache lookup
// Question: a factory that caches its artifact per member still pays, on every hit, for whatever sits in front of the
// lookup - the reflection used to build the key, and the display class of a capturing lambda handed to GetOrAdd (the
// compiler allocates it at method entry, so the hit path pays for it too). How much is left once the lookup goes first?
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class CacheLookupOrderBenchmark
{
    private static readonly ConcurrentDictionary<PropertyInfo, Func<object, object?>> Cache = new();

    private PropertyInfo property = default!;

    [GlobalSetup]
    public void Setup()
    {
        property = typeof(AccessorTarget).GetProperty(nameof(AccessorTarget.Value))!;
        _ = ReflectThenLookup();
    }

    // ❌ Probe the type graph and build the closure first, then look the cache up
    [Benchmark(Baseline = true)]
    public Func<object, object?> ReflectThenLookup()
    {
        var holder = FindHolderProperty(property.PropertyType);
        return Cache.GetOrAdd(property, _ => BuildAccessor(property, holder));
    }

    // ⚠️ Lookup first, but a capturing lambda remains in the method - the display class is allocated on entry
    [Benchmark]
    public Func<object, object?> LookupFirstCapturingLambda()
    {
        var key = property;
        if (Cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var holder = FindHolderProperty(key.PropertyType);
        return Cache.GetOrAdd(key, _ => BuildAccessor(key, holder));
    }

    // ✅ Lookup first and pass the state to a static lambda - nothing allocates on the hit path
    [Benchmark]
    public Func<object, object?> LookupFirstStaticLambda()
    {
        if (Cache.TryGetValue(property, out var cached))
        {
            return cached;
        }

        return Cache.GetOrAdd(
            property,
            static (key, state) => BuildAccessor(key, FindHolderProperty(state)),
            property.PropertyType);
    }

    public static void Verify()
    {
        var benchmark = new CacheLookupOrderBenchmark();
        benchmark.Setup();
        var target = new AccessorTarget { Value = 4200 };
        var accessors = new[]
        {
            benchmark.ReflectThenLookup(),
            benchmark.LookupFirstCapturingLambda(),
            benchmark.LookupFirstStaticLambda(),
        };
        foreach (var accessor in accessors)
        {
            if (accessor(target) is not 4200)
            {
                throw new InvalidOperationException("Verify failed. CacheLookupOrder");
            }
        }

        if (!ReferenceEquals(accessors[0], accessors[1]) || !ReferenceEquals(accessors[0], accessors[2]))
        {
            throw new InvalidOperationException("Verify failed. CacheLookupOrder identity");
        }
    }

    private static PropertyInfo? FindHolderProperty(Type type)
    {
        // Stands in for "is this a value holder" probes: GetInterfaces copies the whole interface list every call
        var holder = Array.Find(type.GetInterfaces(), static x => x.IsGenericType && (x.GetGenericTypeDefinition() == typeof(IEquatable<>)));
        return holder?.GetRuntimeProperty("Value");
    }

    private static Func<object, object?> BuildAccessor(PropertyInfo property, PropertyInfo? holder)
    {
        var getter = property.GetMethod!;
        return holder is null
            ? x => getter.Invoke(x, null)
            : x => holder.GetValue(getter.Invoke(x, null));
    }
}

// TYP-08 study (isolated): type metadata APIs that copy an array on every call
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class TypeMetadataLookupBenchmark
{
    private static readonly Type[] ConstructorParameters = [typeof(int)];

    private static readonly ConcurrentDictionary<Type, Type?> UnderlyingTypes = new();

    private static readonly ConcurrentDictionary<Type, Type?> ElementTypes = new();

    private static readonly ConcurrentDictionary<Type, ConstructorInfo?> Constructors = new();

    private Type nullableType = default!;

    private Type collectionType = default!;

    [GlobalSetup]
    public void Setup()
    {
        nullableType = typeof(int?);
        collectionType = typeof(List<int>);
        _ = CachedUnderlyingType();
        _ = CachedElementType();
        _ = CachedConstructor();
    }

    // ❌ Nullable.GetUnderlyingType copies the generic argument array every call
    [Benchmark(Baseline = true)]
    public Type? GetUnderlyingTypeEveryCall() => Nullable.GetUnderlyingType(nullableType);

    // ✅ Decide once per type
    [Benchmark]
    public Type? CachedUnderlyingType() => UnderlyingTypes.GetOrAdd(nullableType, static x => Nullable.GetUnderlyingType(x));

    // ❌ GetInterfaces copies the interface list every call (int implements more than 30)
    [Benchmark]
    public Type? GetElementTypeEveryCall() => FindElementType(collectionType);

    // ✅ Decide once per type
    [Benchmark]
    public Type? CachedElementType() => ElementTypes.GetOrAdd(collectionType, static x => FindElementType(x));

    // ❌ GetConstructor allocates the Type[] of the signature every call
    [Benchmark]
    public ConstructorInfo? GetConstructorEveryCall() => typeof(AccessorTarget).GetConstructor([typeof(int)]);

    // ✅ Reuse the signature array, or cache the result
    [Benchmark]
    public ConstructorInfo? ReusedSignatureArray() => typeof(AccessorTarget).GetConstructor(ConstructorParameters);

    [Benchmark]
    public ConstructorInfo? CachedConstructor() => Constructors.GetOrAdd(typeof(AccessorTarget), static x => x.GetConstructor(ConstructorParameters));

    public static void Verify()
    {
        var benchmark = new TypeMetadataLookupBenchmark();
        benchmark.Setup();
        if ((benchmark.GetUnderlyingTypeEveryCall() != typeof(int)) ||
            (benchmark.CachedUnderlyingType() != typeof(int)) ||
            (benchmark.GetElementTypeEveryCall() != typeof(int)) ||
            (benchmark.CachedElementType() != typeof(int)) ||
            (benchmark.GetConstructorEveryCall() is null) ||
            (benchmark.ReusedSignatureArray() != benchmark.GetConstructorEveryCall()) ||
            (benchmark.CachedConstructor() != benchmark.GetConstructorEveryCall()))
        {
            throw new InvalidOperationException("Verify failed. TypeMetadataLookup");
        }
    }

    private static Type? FindElementType(Type type)
    {
        var enumerable = Array.Find(type.GetInterfaces(), static x => x.IsGenericType && (x.GetGenericTypeDefinition() == typeof(IEnumerable<>)));
        return enumerable?.GenericTypeArguments[0];
    }
}
