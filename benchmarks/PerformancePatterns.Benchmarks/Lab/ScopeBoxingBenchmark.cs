namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// STK-11 study: the return type of a using scope
// Question: a Begin()/BeginScope() helper usually returns IDisposable, which boxes the scope struct. Tier1 escape
// analysis can remove that box when the whole scope is inlined into the caller, so the obvious synchronous `using`
// measures 0 B - but the box comes back wherever the scope escapes: a non-inlined helper, a field, and the state
// machine of an async method. Does returning the struct type remove it in every shape?
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class ScopeBoxingBenchmark
{
    private readonly ScopeOwner owner = new();

    // ❌ The scope struct is boxed into IDisposable (removed by escape analysis when inlined)
    [Benchmark(Baseline = true)]
    public int BoxedScope()
    {
        using (BeginBoxed(owner))
        {
        }

        return owner.Count;
    }

    // ❌ The box escapes through the non-inlined call and is allocated for real
    [Benchmark]
    public int BoxedScopeNoInline()
    {
        using (BeginBoxedNoInline(owner))
        {
        }

        return owner.Count;
    }

    // ✅ The struct type is returned, so there is nothing to box
    [Benchmark]
    public int StructScope()
    {
        using (BeginStruct(owner))
        {
        }

        return owner.Count;
    }

    [Benchmark]
    public int StructScopeNoInline()
    {
        using (BeginStructNoInline(owner))
        {
        }

        return owner.Count;
    }

    // ❌ A delegate-carrying scope object: the delegate and the holder are both allocated
    [Benchmark]
    public int DelegateScope()
    {
        using (new DelegateScope(owner.Release))
        {
        }

        return owner.Count;
    }

    // ❌ The scope is stored in the async state machine, so escape analysis cannot remove the box
    [Benchmark]
    public async Task<int> BoxedScopeAsync()
    {
        using (BeginBoxed(owner))
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }

        return owner.Count;
    }

    // ✅ The struct lives in the state machine without a box
    [Benchmark]
    public async Task<int> StructScopeAsync()
    {
        using (BeginStruct(owner))
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }

        return owner.Count;
    }

    public static void Verify()
    {
        var owner = new ScopeOwner();
        using (BeginBoxed(owner))
        {
            if (owner.Count != 0)
            {
                throw new InvalidOperationException("Verify failed. ScopeBoxing enter");
            }
        }

        using (BeginStruct(owner))
        {
        }

        using (BeginBoxedNoInline(owner))
        {
        }

        using (BeginStructNoInline(owner))
        {
        }

        using (new DelegateScope(owner.Release))
        {
        }

        if (owner.Count != 5)
        {
            throw new InvalidOperationException($"Verify failed. ScopeBoxing count=[{owner.Count}]");
        }

        // Disposing the struct scope twice must release twice, exactly like the boxed form
        var scope = BeginStruct(owner);
        scope.Dispose();
        scope.Dispose();
        if (owner.Count != 7)
        {
            throw new InvalidOperationException($"Verify failed. ScopeBoxing double dispose count=[{owner.Count}]");
        }

        // A default struct scope must do nothing instead of throwing
        default(OwnerScope).Dispose();
    }

    // Public on purpose: this is the shape of a library's Begin() helper, and the return type is what is being studied
    public static IDisposable BeginBoxed(ScopeOwner owner) => new OwnerScope(owner);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IDisposable BeginBoxedNoInline(ScopeOwner owner) => new OwnerScope(owner);

    public static OwnerScope BeginStruct(ScopeOwner owner) => new(owner);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static OwnerScope BeginStructNoInline(ScopeOwner owner) => new(owner);
}

public sealed class ScopeOwner
{
    public int Count { get; private set; }

    public void Release() => Count++;
}

public readonly struct OwnerScope(ScopeOwner owner) : IDisposable
{
    // Tolerates default(OwnerScope) so that the struct behaves like the boxed form
    public void Dispose() => owner?.Release();
}

internal sealed class DelegateScope(Action release) : IDisposable
{
    public void Dispose() => release();
}
