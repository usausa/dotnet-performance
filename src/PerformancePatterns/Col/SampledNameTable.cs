namespace PerformancePatterns.Col;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// BIT-02 / COL-04: 既知の名前集合に特化した読み取り専用ルックアップ。
/// 長さ + 先頭 / 中央 / 末尾の 3 文字だけから O(1) のサンプリングハッシュを求め(BIT-02)、
/// 2 の累乗サイズ + マスク(BIT-03)でバケットを引き、バケット内は Ordinal 比較で確定する。
/// キーは <see cref="ReadOnlySpan{T}"/> のまま照合できるため string 化が不要(COL-03 と同じ狙い)。
/// </summary>
public sealed class SampledNameTable<TValue>
{
    private readonly Bucket[] buckets;

    private readonly int mask;

    public SampledNameTable(IEnumerable<KeyValuePair<string, TValue>> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var source = entries.ToArray();
        Count = source.Length;

        // 意図的に疎(要素数の 2 倍以上)にしてバケット内の線形探索を 1 件に近づける
        var size = (int)BitOperations.RoundUpToPowerOf2((uint)Math.Max(source.Length * 2, 4));
        mask = size - 1;

        var work = new List<KeyValuePair<string, TValue>>?[size];
        foreach (var pair in source)
        {
            var index = CalculateHash(pair.Key.AsSpan()) & mask;
            (work[index] ??= []).Add(pair);
        }

        buckets = new Bucket[size];
        for (var i = 0; i < size; i++)
        {
            var items = work[i];
            if (items is null)
            {
                buckets[i] = new Bucket([], []);
                continue;
            }

            var keys = new string[items.Count];
            var values = new TValue[items.Count];
            for (var j = 0; j < items.Count; j++)
            {
                keys[j] = items[j].Key;
                values[j] = items[j].Value;
            }

            buckets[i] = new Bucket(keys, values);
        }
    }

    public int Count { get; }

    /// <summary>長さと 3 文字だけを見る O(1) ハッシュ。衝突は呼び出し側の完全一致比較で解決する。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CalculateHash(ReadOnlySpan<char> value)
    {
        var length = value.Length;
        if (length == 0)
        {
            return 0;
        }

        ref var head = ref MemoryMarshal.GetReference(value);
        var first = Unsafe.Add(ref head, 0);
        var middle = Unsafe.Add(ref head, length >> 1);
        var last = Unsafe.Add(ref head, length - 1);
        return (length << 16) ^ (first << 8) ^ (middle << 4) ^ last;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(ReadOnlySpan<char> key, [MaybeNullWhen(false)] out TValue value)
    {
        ref var bucket = ref buckets[CalculateHash(key) & mask];
        var keys = bucket.Keys;
        for (var i = 0; i < keys.Length; i++)
        {
            // ハッシュ一致後は必ず完全一致で確定させる(サンプリングハッシュは衝突しうる)
            if (key.SequenceEqual(keys[i]))
            {
                value = bucket.Values[i];
                return true;
            }
        }

        value = default;
        return false;
    }

    private readonly struct Bucket(string[] keys, TValue[] values)
    {
        public string[] Keys { get; } = keys;

        public TValue[] Values { get; } = values;
    }
}
