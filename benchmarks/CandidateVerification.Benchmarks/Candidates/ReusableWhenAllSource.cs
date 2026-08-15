namespace CandidateVerification.Benchmarks.Candidates;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;

// C-07 full-verification implementation: pooled IValueTaskSource fan-in for ValueTask fan-out.
// Synchronously-completed tasks are folded away; a source is rented only when at least one task is pending.
// Verification-grade: handler exceptions are not captured (benchmark handlers never throw) — a production
// version must record the first exception and complete with SetException.
public sealed class ReusableWhenAllSource : IValueTaskSource
{
    private static readonly ConcurrentQueue<ReusableWhenAllSource> Pool = new();

    private ManualResetValueTaskSourceCore<bool> core;

    private int remaining;

    private ReusableWhenAllSource()
    {
    }

    public static ValueTask WhenAll(Func<ValueTask>[] handlers)
    {
        // Single pass: invoke each handler, fold completed tasks, attach a pooled awaiter node to each pending one.
        // remaining starts at 1 (self reservation) so completions during the loop cannot fire early.
        ReusableWhenAllSource? source = null;
        foreach (var handler in handlers)
        {
            var task = handler();
            if (task.IsCompletedSuccessfully)
            {
                ConsumeCompleted(task);
                continue;
            }

            source ??= Rent();
            source.AddPending(task);
        }

        if (source is null)
        {
            return default;   // all-sync: nothing allocated, nothing rented
        }

        return source.Finish();
    }

    public void GetResult(short token)
    {
        try
        {
            core.GetResult(token);
        }
        finally
        {
            core.Reset();
            Pool.Enqueue(this);
        }
    }

    public ValueTaskSourceStatus GetStatus(short token) => core.GetStatus(token);

    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        => core.OnCompleted(continuation, state, token, flags);

    // Completed-successfully tasks only: GetResult is a no-op observation, isolated here for analyzer clarity
    private static void ConsumeCompleted(in ValueTask task) => task.GetAwaiter().GetResult();

    private static ReusableWhenAllSource Rent()
    {
        if (!Pool.TryDequeue(out var source))
        {
            source = new ReusableWhenAllSource();
        }

        source.remaining = 1;
        return source;
    }

    private void AddPending(ValueTask task)
    {
        Interlocked.Increment(ref remaining);
        AwaiterNode.Rent(this).Attach(task);
    }

    private ValueTask Finish()
    {
        var token = core.Version;
        Signal();   // release the self reservation; if everything already completed this sets the result
        return new ValueTask(this, token);
    }

    private void Signal()
    {
        if (Interlocked.Decrement(ref remaining) == 0)
        {
            core.SetResult(true);
        }
    }

    private sealed class AwaiterNode
    {
        private static readonly ConcurrentQueue<AwaiterNode> NodePool = new();

        private readonly Action continuation;

        private ReusableWhenAllSource parent = default!;

        private ValueTaskAwaiter awaiter;

        private AwaiterNode()
        {
            continuation = OnCompleted;   // method-group delegate allocated once per node lifetime
        }

        public static AwaiterNode Rent(ReusableWhenAllSource parent)
        {
            if (!NodePool.TryDequeue(out var node))
            {
                node = new AwaiterNode();
            }

            node.parent = parent;
            return node;
        }

        public void Attach(ValueTask task)
        {
            awaiter = task.GetAwaiter();
            awaiter.UnsafeOnCompleted(continuation);
        }

        private void OnCompleted()
        {
            var source = parent;
            try
            {
                awaiter.GetResult();
            }
            finally
            {
                parent = default!;
                awaiter = default;
                NodePool.Enqueue(this);
                source.Signal();
            }
        }
    }
}
