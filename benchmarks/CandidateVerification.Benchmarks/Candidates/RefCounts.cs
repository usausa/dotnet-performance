namespace CandidateVerification.Benchmarks.Candidates;

// C-06 baseline: classic CAS retry loop ("increment if alive")
public sealed class CasLoopRefCount
{
    private int refCount = 1;

    public bool Disposed { get; private set; }

    public bool TryRetain()
    {
        while (true)
        {
            var current = Volatile.Read(ref refCount);
            if (current <= 0)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref refCount, current + 1, current) == current)
            {
                return true;
            }
        }
    }

    public void Release()
    {
        if (Interlocked.Decrement(ref refCount) == 0)
        {
            Disposed = true;
        }
    }
}

// C-06 candidate: optimistic increment-first retain, death is a large negative bias stamp
public sealed class OptimisticRefCount
{
    private const int DeadBias = int.MinValue / 2;

    private int refCount = 1;

    public bool Disposed { get; private set; }

    public bool TryRetain()
    {
        // Single fetch-add, no retry storm under contention
        var result = Interlocked.Increment(ref refCount);
        if (result >= 1)
        {
            return true;
        }

        // The bias is large enough that a late increment can never resurrect a dead entry
        Interlocked.Decrement(ref refCount);
        return false;
    }

    public void Release()
    {
        if ((Interlocked.Decrement(ref refCount) == 0) &&
            (Interlocked.CompareExchange(ref refCount, DeadBias, 0) == 0))
        {
            Disposed = true;
        }
    }
}
