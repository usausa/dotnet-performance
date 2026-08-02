# 🏭 Source Generator Codegen Pattern Catalog

[日本語](generated-code-patterns.ja.md) | **English**

A catalog of **what code a Source Generator should emit in order to achieve performance**.
It records the **shape of the code to emit** and the measured evidence that the shape is fast, rather than how to implement the generator (the Roslyn API).
Every code example here is "what the generator's output looks like"; hand-writing the same shape gives the same performance (the measurements were taken with the hand-written form).

Corresponding main pattern: **GEN-02** in [README](../README.md). For where this sits in the AOT context, see **AOTS-01** (root-cause fix) and **AOTS-08** (dual path) in [aot-compatibility.md](aot-compatibility.md).

---

## 🧭 Three principles of codegen design

1. **Move runtime resolution to build time** — of the dictionary lookups, reflection, hash computation, and string assembly involved, bake whatever can be settled at generation time into code (constants, switch, direct `new`)
2. **Branch on count and shape** — the generator knows the target's count, types, and layout. Do at generation time the "switch implementations based on N" that a runtime library cannot do
3. **Emit combinations of measured patterns** — build the body of the generated code out of this catalog's adopted patterns, and never include rejected patterns (R-01–R-17)

---

## 1. Name → index resolution (column names, property names, key strings)

**Scenario:** Resolving a known set of strings — DB column names, property names, JSON keys — to a number.

**What to generate:** Branch on the count.

```csharp
// Count <= 4: Equals chain (generated form)
public static int GetIndex(ReadOnlySpan<char> name)
{
    if (name.SequenceEqual("Id")) return 0;
    if (name.SequenceEqual("Name")) return 1;
    if (name.SequenceEqual("Flag")) return 2;
    return -1;
}

// Count >= 5: sampling-hash switch (hash constants are computed and baked in at generation time)
public static int GetIndex(ReadOnlySpan<char> name)
{
    switch (SamplingHash.Calculate(name))   // (length << 16) ^ (first << 8) ^ (mid << 4) ^ last
    {
        case 0x00243C56 when name.SequenceEqual("CreatedAt"): return 2;
        case 0x00159F11 when name.SequenceEqual("Name"): return 1;
        // ...
    }
    return -1;
}
```

**Why it is faster:** Unlike a general hash that reads every character, sampling the length plus 3 characters narrows the candidates, and the confirming comparison is a single SIMD-accelerated `SequenceEqual`. Since the hash constants become JIT constants, the switch turns into a jump table.

**Measured evidence:**

- The sampling hash table (runtime version) runs at 0.56–0.84x of `Dictionary`, and with Span keys beats even `FrozenDictionary` at every size → [COL-04-SampledNameTable.md](../benchmarks/results/COL-04-SampledNameTable.md)
- Linear search (the runtime version of the Equals chain) only wins up to 4 entries, degrading to 2.73x at 16 → same record (the basis for the branch threshold)
- Because the positions are chosen at generation time, a colliding key set can be recovered by moving the sampling positions (a degree of freedom unique to codegen that the runtime version does not have)

**Caveats:** For case-insensitive matching, upper-case the sampled characters before computing the hash and make the confirming comparison `OrdinalIgnoreCase`. Comparisons are always ordinal (TXT-03).

---

## 2. Baking in per-type artifacts (SQL fragments, type names, format strings)

**Scenario:** SQL that is fixed per type, type names for logging, serialization key names, and so on.

**What to generate:** Do not assemble at runtime — **write it directly into const / static readonly**.

```csharp
// Generated form: no runtime StringBuilder and no dictionary lookup
internal static class OrderSql
{
    public const string Insert = "INSERT INTO Order (Id, Name, Amount, CreatedAt) VALUES (@Id, @Name, @Amount, @CreatedAt)";
}

// If UTF-8 is needed, bake it in as a u8 literal (no runtime encoding)
internal static class OrderJson
{
    public static ReadOnlySpan<byte> IdKey => "\"id\":"u8;
}
```

**Why it is faster:** Reading is nothing but a constant load. A `ReadOnlySpan<byte>` property backed by a u8 literal points straight into the assembly's data section, so allocation is zero.

**Measured evidence:** Assembling every time costs 116 ns + 760 B → dictionary cache 4.8 ns → **generic static read 0.09 ns / 6 B of code**. Generated code can push the fastest form (the static read) one step further, down to a const → [TYP-06-StaticArtifact.md](../benchmarks/results/TYP-06-StaticArtifact.md)

**Caveats:** Artifacts that depend on generic type arguments should be generated in the `static class Cache<T>` form (TYP-04 / TYP-06). Design the type initializer so it never throws.

---

## 3. DB row mappers

**Scenario:** Mapping `DbDataReader` → POCO (doing at build time what Dapper-style libraries do at runtime).

**What to generate:** An ordinal struct + one-pass column resolution + typed getters.

```csharp
// Generated form
private readonly struct OrderOrdinals(int id, int name, int flag)
{
    public readonly int Id = id;
    public readonly int Name = name;
    public readonly int Flag = flag;
}

public static OrderOrdinals ResolveOrdinals(DbDataReader reader)
{
    int id = -1, name = -1, flag = -1;
    for (var i = 0; i < reader.FieldCount; i++)
    {
        var column = reader.GetName(i);
        // For many columns, generate and use the name switch from scenario 1
        if (string.Equals(column, "Id", StringComparison.OrdinalIgnoreCase)) { id = i; }
        else if (string.Equals(column, "Name", StringComparison.OrdinalIgnoreCase)) { name = i; }
        else if (string.Equals(column, "Flag", StringComparison.OrdinalIgnoreCase)) { flag = i; }
    }
    return new OrderOrdinals(id, name, flag);
}

public static Order Map(DbDataReader reader, in OrderOrdinals ordinals) => new()
{
    Id = reader.GetInt32(ordinals.Id),        // Do not generate GetValue + cast (boxing)
    Name = reader.GetString(ordinals.Name),
    Flag = reader.GetBoolean(ordinals.Flag),
};
```

**Why it is faster:** Column resolution happens once per reader, and the row loop becomes nothing but struct field reads and direct calls to typed getters.

**Measured evidence:** `GetOrdinal` per row is 11.3 ns/row → **ordinal struct passed by `in`, 1.42 ns/row (0.13x)**, code size 2,225 → 537 B. Generating `GetValue` + cast instead costs 7.18 ns/row plus **48 B/row of boxing** → [DAT-01-OrdinalResolve.md](../benchmarks/results/DAT-01-OrdinalResolve.md)

**Caveats:** If missing columns are allowed, leave the ordinal at -1 and generate a branch on the Map side (do not use `GetOrdinal`, which throws). For enum columns, generate code that reads the underlying type and casts.

---

## 4. Factory / DI resolution

**Scenario:** Constructing instances from a dependency graph of registered types (doing at build time what a DI container does with Emit).

**What to generate:** **Inline the dependency graph into direct `new` expressions.** Do not generate chains of child factory calls.

```csharp
// ✅ Generated form: the graph expanded into a single method (singletons are static readonly reads)
internal static class ServiceFactory
{
    private static readonly DepA SharedDepA = new();

    public static Service Create() => new(SharedDepA, new DepB(new DepC()));
}

// ❌ Never generate this: carrying child factories around as Func and calling them
public static Service Create() => new(
    (DepA)childFactories[0](),   // A chain of delegate calls + castclass
    (DepB)childFactories[1]());
```

**Why it is faster:** The call chains, castclass, and delegate indirection all disappear, leaving direct calls whose constructors the JIT can inline.

**Measured evidence:** In GEN-01 (the same scenario on the Emit side), a child factory chain costs **2.3x** versus direct code, and a closure-array target 1.5x. The direct-code equivalent (DirectLambda) is 6.23 ns → [GEN-01-EmitStrategy.md](../benchmarks/results/GEN-01-EmitStrategy.md). Generated code can emit a shape equivalent to Emit's best form (Holder field, 6.55 ns), AOT-safely.

**Caveats:** Settle lifetimes (singleton / per-call) at generation time and express them in the shape (singleton = static readonly, per-call = direct `new`). Generate a `Dictionary<Type, Func<object>>` only for the entry points that genuinely need resolution from a runtime `Type`, with its values pointing at the direct factories above (note the measurement showing TYP-01's runtime Type path is slower than a plain dictionary — routing statically known calls through a generic API comes first).

---

## 5. Formatting and serialization

**Scenario:** Code that writes values out as text or binary — JSON, logs, fixed-width records, and so on.

**What to generate:**

```csharp
// ✅ Call TryFormat directly for numbers and dates (no intermediate string)
value.TryFormat(destination, out written);

// ✅ CopyTo from u8 literals for known keys and separators (no runtime encoding)
"\"name\":"u8.CopyTo(destination);

// ✅ Generate string.Create for concatenations whose length can be precomputed
return string.Create(length, state, static (span, s) => { /* a sequence of CopyTo calls */ });

// ✅ For fixed formats (dates and the like), generate two-digit table lookups (TXT-01)
```

**Why it is faster:** Intermediate strings and byte[]s disappear, and writing becomes a sequence of direct buffer writes.

**Measured evidence:**

- `string.Create` runs at 0.57x of interpolation and allocates only the result → [TXT-07-StringCreate.md](../benchmarks/results/TXT-07-StringCreate.md)
- Table-driven fixed formats → [TXT-01-Utf8DateTimeFormatter.md](../benchmarks/results/TXT-01-Utf8DateTimeFormatter.md)
- **Never generate hand-written digit-packing loops** (2.5–4.8x slower than `TryFormat`, R-16) → [TXT-09-FixedFieldFormat.md](../benchmarks/results/TXT-09-FixedFieldFormat.md)

**Caveats:** Make `IBufferWriter<T>` (BUF-02) or direct Span writes (SEQ-02) the default sink for sequential writing in generated code.

---

## 6. enum specialization (TryParse / ToString)

**Scenario:** Name ⇔ value conversion for a known enum.

**What to generate:** Apply scenario 1 (the name switch), and make ToString a switch returning constants.

```csharp
// Generated form
public static bool TryParse(ReadOnlySpan<char> name, out Color value)
{
    // Equals chain or sampling-hash switch depending on the count (same shape as scenario 1)
}

public static string FastToString(this Color value) => value switch
{
    Color.Red => "Red",       // Constant return (no runtime name resolution, no allocation)
    Color.Green => "Green",
    _ => value.ToString(),    // Unknown values fall back to the BCL
};
```

**Measured evidence:** The name resolution part is the same shape as scenario 1 (it reduces to the COL-04 measurements). Returning constants from ToString structurally guarantees zero allocation (`Enum.ToString` involves string creation).

**Caveats:** First check whether the BCL's `Enum.TryParse<T>(ReadOnlySpan<char>, ...)` is enough before generating anything. Do not generate code of the form `(T)Enum.Parse(typeof(T), name)` (boxing, plus AOTP-05 style concerns).

---

## 7. Collection conversion

**Scenario:** Code that converts an array / List / DB result into a DTO list.

**What to generate:** Assume the count is known and settle the allocation up front.

```csharp
// Generated form: fixed capacity + SetCount + direct Span writes (COL-01 / COL-06)
var list = new List<TDestination>(source.Length);
CollectionsMarshal.SetCount(list, source.Length);
var span = CollectionsMarshal.AsSpan(list);
for (var i = 0; i < source.Length; i++)
{
    span[i] = Convert(source[i]);
}
```

**Measured evidence:** SetCount + direct Span writes run at 0.21–0.27x of an Add loop, with zero allocation when reused → [COL-06-CollectionConvert.md](../benchmarks/results/COL-06-CollectionConvert.md). For `ImmutableArray` from a contiguous region, generate `ToImmutableArray()` (going through a Builder is 2.5–3.4x slower).

---

## 8. Change notification and events (generating INotifyPropertyChanged, etc.)

**Scenario:** Generating property change notification and event-raising code.

**What to generate:**

```csharp
// ✅ Bake PropertyChangedEventArgs into static readonly (zero allocation per raise)
private static readonly PropertyChangedEventArgs NameChangedArgs = new(nameof(Name));

public string Name
{
    get => name;
    set
    {
        if (!string.Equals(name, value, StringComparison.Ordinal))
        {
            name = value;
            PropertyChanged?.Invoke(this, NameChangedArgs);   // No allocation
        }
    }
}
```

**Why it is faster:** The `new PropertyChangedEventArgs(...)` on every raise disappears (structurally zero allocation). If you generate your own event subscription structure, pick the shape based on the number of subscribers.

**Measured evidence:** With a single subscriber a multicast delegate is fastest (the array form is 2.87x slower), but **from two subscribers up the immutable array form takes the lead** (0.36x at four) → [DSP-03-HandlerList.md](../benchmarks/results/DSP-03-HandlerList.md). Generate callbacks in the static lambda + TState form (DSP-04).

---

## 9. ❌ Code you must never generate (anti-generation list)

Shapes that tend to creep into generated code because they "look fast", but which measurement and generated-code inspection have **confirmed to be ineffective or outright counterproductive**. Generators (and AI code generation) must not emit these.

| Shape you must never generate | Reason (measured) | Record |
|---|---|---|
| static readonly caching of `typeof(X)` | Generated code matches exactly at Tier1; before promotion the cached version is actually worse | R-01 |
| Unconditionally turning read-only dictionaries into `FrozenDictionary` | Construction 15–20x; lookups can invert too, depending on the key set | R-08 |
| readonly on instance fields for performance | Generated code confirmed identical (apart from the offset) | R-10 |
| Replacing `Span.CopyTo` with `Unsafe.CopyBlockUnaligned` | Variable lengths reach the same Memmove (measurement noise); you only lose safety | R-14 |
| Hand-written digit-packing format loops (right-align shift, reverse-order writes) | 2.5–4.8x slower than `TryFormat` + `Fill` | R-16 |
| Emitting delegate Invoke with `call` instead of `callvirt` | JIT check showed the generated code matching exactly at 68 instructions / 229 B | R-17 |
| Converting single-Span loops to `GetReference` + `Unsafe.Add` | A standard for already has bounds checks eliminated; going manual is 1.07–1.13x slower and a bug source | R-02 |
| Generating hand-rolled hash loops (FNV-1a and the like) | Slower than `string.GetHashCode` from 64 characters up; use XxHash3 or sampling (scenario 1) | [BIT-05](../benchmarks/results/BIT-05-XxHash3.md) |
| Generating a runtime `Type`-keyed dictionary as the primary path | The runtime Type path is slower than a plain Dictionary (1.93x); take a generic API and resolve statically | [TYP-01](../benchmarks/results/TYP-01-TypeMap.md) |

For details, see [rejected-patterns.md](rejected-patterns.md).

---

## 🧪 Verifying generated code

- Apply this catalog's verification process to generated code as well: always provide an **equivalence test** (the generated form matches the results of the straightforward implementation) — the same caveat as GEN-01
- Measure before recording any performance claim. If the measurement falls within measurement noise, go down to the generated code (JitDisasm) to distinguish "➖ measurement noise" from "no difference" (the decision criteria in [benchmark-methodology.md](benchmark-methodology.md))
- If you also ship an Emit fast path for JIT environments, branch on `RuntimeFeature.IsDynamicCodeCompiled` (AOTS-08). But as the GEN-01 measurements show, **directly generated code is on par with Emit's best form**, so a dual path is only needed for dynamic scenarios that cannot be generated
