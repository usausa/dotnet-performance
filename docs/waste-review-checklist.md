# 🧹 Reviewing a library for waste

The pattern catalog in the [README](../README.md) covers how to write something fast. This document covers **where to look in code that already ships** to find waste, and is meant as the entry point for such a review.

**Where the reference numbers come from:** the numbers in this document were measured in 2026-11 while reviewing seven shipped libraries (four UI, plus conversion, MVVM and core). They were taken on a **Zen 3 machine under the .NET 10 JIT**, not on the catalog's reference environment (HX 370 / x86-64-v4). Adoption decisions belong to each pattern's own measurement.

---

## 🔍 The angles

| # | Angle | Typical symptom | Pattern |
|---|---|---|---|
| W-01 | Rebuilding the whole thing | The cost of one change scales with the size of the collection; the notification arguments already say what changed | [below](#w-01-rebuilding-the-whole-thing) |
| W-02 | Building and notifying when nothing changed | The rebuilt result equals the previous one, yet the diff arrays and dictionaries are still built; an empty update still notifies | [below](#w-02-building-and-notifying-when-nothing-changed) / COL-07 |
| W-03 | Building arguments with no listener | The event arguments or result holder are built before checking whether anyone subscribed | [below](#w-03-building-arguments-with-no-listener) / DSP-03 |
| W-04 | Returning a rebuilt copy of the same thing | A conversion returns a new string or array even when the result equals the input | [below](#w-04-returning-a-rebuilt-copy-of-the-same-thing) |
| W-05 | Work in front of the cache lookup | Reflection, key building and a closure run even on a hit | TYP-08 / TYP-04 |
| W-06 | Not remembering absence | Every miss pays the lock, the type-metadata copy and a fresh empty instance | COL-08 |
| W-07 | Querying type metadata every call | `GetUnderlyingType` / `GetInterfaces` / `GenericTypeArguments` / `GetConstructor(Type[])` all copy an array | TYP-08 |
| W-08 | An argument array per reflection call | `Invoke(object?[])` / `SetValue`. It stays on the AOT path, where Emit is unavailable | GEN-03 / GEN-01 |
| W-09 | Exceptions used as control flow | Particular inputs go through try/catch every time; a BCL API with no Try version is wrapped | TXT-03 |
| W-10 | Leaving it to the base class default | `Stream`'s `ReadByte` / `CopyTo` / async members. The defaults were written for a different device | SEQ-06 |
| W-11 | An async shell over synchronous work | A state machine, a `TaskCompletionSource` or an `AsTask()` for work that completes synchronously | ASY-01 / ASY-05 |
| W-12 | Scopes and temporaries that box | A `Begin()` returning `IDisposable`, a scope carrying a delegate, an `object`-typed hole | STK-11 / STK-05 / TXT-07 |
| W-13 | A boxed enumerator | Iterating as `IEnumerable<T>`. Anything other than `List<T>` and arrays keeps the box | COL-05 |
| W-14 | Allocating a non-pooled array | An `array[..n]` or a `ToArray()` sitting in the middle of a grow path | MEM-06 / BUF-01 |
| W-15 | Running while nothing is showing | Timers and subscriptions stay alive while the view is gone | [below](#w-15-running-while-nothing-is-showing) |
| W-16 | A table per consumer | The same conversion table or cache exists once per library | [below](#w-16-a-table-per-consumer) / TYP-04 |
| W-17 | Returning a view that reconverts on every read | The conversion result is a lazy view that reconverts each element on every read and every enumeration; `Contains` / `IndexOf` throw | COL-06 |
| W-18 | Hand-composing what a BCL API already does | A wait with a timeout composed from `CancellationTokenSource` + `Task.Delay` + `WhenAny` (`Task.WaitAsync` exists) | ASY-05 |

Angles that map to a catalog pattern ID have their measurements, preconditions and 💥 notes in that pattern. What follows is how to spot the angles that have no pattern ID, because they are shapes of a design rather than a choice of API.

---

## 🧭 The angles with no pattern ID

### W-01 Rebuilding the whole thing

- **How to spot it:** in code that handles a collection-changed notification, look for a full walk of the source that rebuilds the internal state while ignoring `NewItems` / `OldItems` on `NotifyCollectionChangedEventArgs`
- **How to fix it:** add and remove only the delta per change kind, and rebuild wholesale only for `Reset` and for notifications that carry no items. For subscription counting, hold the count in a `Dictionary<T, int>`
- **Reference numbers:** adding 5,000 items in one batch went **837 ms / 697 MB → 8.1 ms / 2.2 MB**. 1,000 `Move` / `Replace` operations over 1,000 items also got 1.5-1.7x faster
- **💥 to check:** it now trusts the notification arguments, so a hand-written collection that reports them incorrectly drifts out of sync (the BCL collections report them correctly). Always keep the `Reset` rebuild path

### W-02 Building and notifying when nothing changed

- **How to spot it:** is there an early exit for "the rebuilt result equals the previous one"? Does one operation notify while every sibling in the same class (the `Clear` family, an empty add) stays silent?
- **How to fix it:** compare the count and the elements first and bail out when equal. When a diff is built, give the dictionary a capacity and fuse the probe with `CollectionsMarshal.GetValueRefOrAddDefault` / `GetValueRefOrNullRef` (COL-07)
- **Reference numbers:** 1,000 no-op `Refresh` calls over 1,000 items went **57.9 ms / 123.5 MB → 8.3 ms / 7.8 MB**. Even reordering changes allocated about half as much
- **💥 to check:** fewer notifications is a behaviour change. Confirm that no UI binding relies on being re-evaluated every time

### W-03 Building arguments with no listener

- **How to spot it:** on the raising side of an event or request, is the argument object built before `Handler is not null` is tested? If the siblings in the same namespace check first, the odd one out is the waste
- **How to fix it:** read the handler once and, when it is null, return the default without building the arguments
- **Reference numbers:** one call with no listener went **3.9-5.0 ns / 24 B → 0.6-0.8 ns / 0 B**
- **💥 to check:** when the result type is a value type, decide what the no-argument path returns (casting `null` back to a value type throws `NullReferenceException`, so return `default` instead)

### W-04 Returning a rebuilt copy of the same thing

- **How to spot it:** in a name conversion, normalization or escape, does it build a new instance even when **the input is already in the target shape**? Check whether the BCL counterpart (`JsonNamingPolicy.CamelCase`, say) returns the input as is
- **How to fix it:** test whether a conversion is needed at all and return the received instance when it is not
- **Reference numbers:** an already-lowercase key went **11.9-13.9 ns / 32-48 B → 1.9-5.7 ns / 0 B**. Over 20,000 random inputs, 14,038 took the return-as-is path
- **💥 to check:** the same instance now comes back, so callers that distinguished them with `ReferenceEquals` see a difference (harmless for an immutable type)

### W-15 Running while nothing is showing

- **How to spot it:** does a behaviour, attached property or subscription start **the moment it is attached**? UI frameworks sometimes keep attachments alive after a view closes, so "stop when detached" alone leaves it running
- **How to fix it:** start and stop on the visibility lifecycle (`Loaded` / `Unloaded` and friends) as well as on detach. If a sibling component in the same library already does this, match it
- **Reference numbers:** not a timing issue but a behaviour bug. The timer kept firing its action after the view closed, and the behaviour, the action and the view model were all kept alive
- **💥 to check:** nothing runs before the view becomes visible, and the interval restarts on return. Also confirm which platforms keep a view `Loaded` while it is in the background

### W-16 A table per consumer

- **How to spot it:** does the same conversion table, cache or reflection exist in the same shape across several libraries or several consumers? If the only difference is something like the default return value, it can be shared
- **How to fix it:** put it one layer down (the shared core library) and have each library just call it. The reflection and its suppression end up in one place together with the table
- **Reference numbers:** four UI libraries each held the same enum-to-description table. Sharing it and revisiting the key took one hit from **20.9-27.5 ns → 14.3-15.0 ns**, against **181-1,104 ns / 120-280 B** with no table
- **💥 to check:** it adds one public API. As an extension method the name can collide with a consumer's own extension and break their build with `CS0121`, so make it a static method

---

## 📏 How to run the review

1. **Heaviest first, one at a time.** Put the options (current shape, candidates, impact, 💥) in a table, decide, then build. Land one item at a time and compare before and after
2. **Compare before and after on a copy.** Build the candidate in a copy of the repository and **compare results, notifications and exceptions line by line** over the same operation sequence (thousands to tens of thousands of randomized steps). Pin down "the same behaviour" before looking at speed
3. **Run the parallelizable checks in parallel, first.** Builds, tests, AOT publishing and the comparisons can all run at once. **Only timing must run one at a time** (parallel runs perturb it; allocation counts are unaffected)
4. **Break it on purpose and confirm the new tests fail.** For each way of breaking it — reverting the change, dropping one condition — record how many tests fail
5. **Re-measure on the built artifact.** Use the artifact you actually built (hash-checked), not the numbers from the prototype
6. **Run static analysis once, batched.** Not per item, but once the changes have settled
7. **Do not make a change only to silence a warning.** Confirm first that behaviour, speed and allocation do not regress

---

## 🚫 Looked at and judged not to be waste

Shapes that the same review examined and decided to leave alone. **Do not touch code of the same shape.**

| Shape | Reason |
|---|---|
| A static table built once (per-type metadata, constant tables) | One build at initialization, never on the hit path (the correct form of TYP-04 / TYP-06) |
| Reflection that only runs at construction time | A negligible share of the total cost (about 0.15% of the construction path in the case measured) |
| A cache keyed by type rather than by value | Faster than comparing values, with no collisions |
| A trigger that subscribes on lifecycle events | The correct form of W-15 |
| Adding a public API to a type with no consumers (a struct enumerator, say) | The gain only appears when `foreach` runs over the concrete type, and the public API and behaviour change |
| A double dictionary probe on a path worth a few ns | Fusing it saves a few ns and costs several suppressions for analyzer false positives |
| A shape where the BCL pays the same cost | Behaving differently from the BCL surprises consumers more (`List<T>.Find` not guarding against mutation, say) |
| Deferring the work while there is no listener (an extension of W-01) | With a UI binding there is always a listener, so there is no gain, and it changes when the filter and the sort run and where their exceptions surface |
