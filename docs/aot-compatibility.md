# 🧩 AOT / Trimming Compatibility Patterns

[日本語](aot-compatibility.ja.md) | **English**

A pattern catalog for building libraries that work under Native AOT and trimming.

- **Incompatible patterns (AOTP)**: implementations to avoid or watch out for, and what to use instead
- **Mitigation patterns (AOTS)**: techniques applied on the library design side
- For whether a given performance pattern is AOT-safe, see the AOT column of the pattern list in the [README](../README.md)

## 🗺️ Overview

| Optimization | What it does | What it breaks | Warnings |
|---|---|---|---|
| Trimming (`PublishTrimmed`) | Removes unused code | Members referenced implicitly through reflection | IL2xxx |
| Native AOT (`PublishAot`) | Ahead-of-time native compilation (includes trimming) | Runtime code generation, plus the above | IL3xxx + IL2xxx |

**Recommended approach for libraries (in priority order):**

1. Design so that reflection and runtime code generation are unnecessary (generic APIs / Source Generator)
2. If you must use them, feed the trimmer information through attributes such as `[DynamicallyAccessedMembers]`
3. For inherently dynamic APIs, declare the incompatibility with `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]`

---

## 🚫 Incompatible patterns (AOTP)

| ID | Pattern | Severity | Typical symptom | Main alternative |
|---|---|:---:|---|---|
| [AOTP-01](#aotp-01-reflectionemit) | Reflection.Emit (DynamicMethod / ILGenerator, etc.) | Critical | `PlatformNotSupportedException` | Source Generator |
| [AOTP-02](#aotp-02-expressiontcompile) | Expression\<T\>.Compile() | Critical | Exception, or crippling slowdown under the interpreter | Source Generator / direct API |
| [AOTP-03](#aotp-03-activatorcreateinstancetype) | Activator.CreateInstance(Type) | High | `MissingMethodException` after trimming | Generic API + attributes |
| [AOTP-04](#aotp-04-makegenerictype--makegenericmethod) | MakeGenericType / MakeGenericMethod | High | Runtime failure for value type combinations (IL3050) | Source Generator / static dispatch |
| [AOTP-05](#aotp-05-metadata-scanning) | GetProperties / GetMethods / GetCustomAttribute | High–Medium | Members missing after trimming | Attributes / Source Generator |
| [AOTP-06](#aotp-06-propertyinfosetvalue--methodinfoinvoke) | PropertyInfo.SetValue / MethodInfo.Invoke | High–Medium | Runtime failure after trimming (IL2026) | Generated accessors / delegate registration |
| [AOTP-07](#aotp-07-string-based-type-resolution-and-dynamic-assembly-loading) | Assembly.GetType(string) / Assembly.LoadFrom | High | Untrackable by the trimmer; unsupported on AOT | Static registration (ModuleInitializer) |
| [AOTP-08](#aotp-08-reflection-based-serialization) | System.Text.Json reflection mode, etc. | High | `NotSupportedException` | JsonSerializerContext (SG) |
| [AOTP-09](#aotp-09-reflection-based-configuration-binding-and-di) | ConfigurationBinder / reflection-based DI | Medium | Binding and resolution failures | Binder SG / generic registration |
| [AOTP-10](#aotp-10-regexoptionscompiled--dynamic-patterns) | RegexOptions.Compiled / dynamic patterns | Low–Medium | Exception or slowdown | \[GeneratedRegex\] |

### AOTP-01: Reflection.Emit

**Problem:** Runtime IL generation through `DynamicMethod` / `ILGenerator` / `TypeBuilder` / `AssemblyBuilder` is entirely unsupported under AOT and throws `PlatformNotSupportedException`.

**Where this shows up:** factory generation in DI containers, dynamic mappers in O/R mappers, getter/setter delegate factories.

**Mitigation:**

| Approach | Details |
|---|---|
| Source Generator (recommended) | Generate the code at compile time with a Roslyn incremental source generator ([AOTS-01](#aots-01-source-generator-root-fix)) |
| Reflection fallback | Ship a slower `PropertyInfo.GetValue/SetValue` based path for AOT |
| Branch on the runtime environment | Use `RuntimeFeature.IsDynamicCodeSupported` to bypass the Emit path ([AOTS-08](#aots-08-dual-paths-via-runtimefeature)) |
| Apply `[RequiresDynamicCode]` | Declare the API AOT-incompatible so consumers get a compile-time warning ([AOTS-06](#aots-06-requiresunreferencedcode--requiresdynamiccode)) |

### AOTP-02: Expression\<T\>.Compile()

**Problem:** Runtime compilation of expression trees does not work under AOT. Native AOT falls back to interpretation and gets dramatically slower, and environments where the IL interpreter is disabled (Blazor WASM AOT, for example) throw.

**Where this shows up:** dynamic generation of property accessors, column mapping in O/R mappers, `FieldIdentifier.Create(expression)`-style APIs.

**Mitigation:** generate accessors statically with a Source Generator / add overloads that take no `Expression` / apply `[RequiresDynamicCode]`.

### AOTP-03: Activator.CreateInstance(Type)

**Problem:** Once trimming removes the constructor, you get a `MissingMethodException`.

**Mitigation:**

| Approach | Details |
|---|---|
| Standardize on generic APIs | Pin the type at compile time with `Create<T>()` ([AOTS-02](#aots-02-standardize-on-generic-apis)) |
| Factory delegate registration | The `Register<T>(Func<T> factory)` pattern ([AOTS-03](#aots-03-factory-delegate-registration)) |
| `[DynamicallyAccessedMembers(PublicConstructors)]` | Apply it to the type parameter or argument to hint the trimmer ([AOTS-04](#aots-04-dynamicallyaccessedmembers)) |

### AOTP-04: MakeGenericType / MakeGenericMethod

**Problem:** AOT cannot produce generic instantiations that do not exist at compile time (IL3050). Reference type arguments usually work through shared generics, but **value type arguments fail at runtime unless the instantiation was generated ahead of time**.

**Where this shows up:** `typeof(Option<>).MakeGenericType(property.PropertyType)`, `method.MakeGenericMethod(targetType)`.

**Mitigation:** enumerate and generate the type combinations actually used at compile time with a Source Generator / static dispatch over known types (switch expression) / apply `[RequiresDynamicCode]`.

### AOTP-05: Metadata scanning

**Problem:** Metadata scanning through `Type.GetProperties()` / `GetMethods()` / `GetCustomAttribute<T>()` silently comes up empty when trimming removes the target members or attributes.

**Mitigation:** instruct the trimmer to keep the metadata with `[DynamicallyAccessedMembers]` / bake attribute and property information into static code at build time with a Source Generator / `rd.xml` (a stopgap, [AOTS-09](#aots-09-rdxml--trimmerrootdescriptor-stopgap)).

### AOTP-06: PropertyInfo.SetValue / MethodInfo.Invoke

**Problem:** Runtime failure once trimming removes the member. Flagged by warning IL2026.

**Mitigation:** generate static accessors with a Source Generator / register delegates up front in `Action<T, TValue>` form / move to an interface contract. When the target is a known non-public member, `[UnsafeAccessor]` (TYP-03 in the [README](../README.md#️-typ-03-unsafeaccessor非公開メンバーへの直接アクセス)) is a reflection-free, AOT-compatible alternative.

### AOTP-07: String-based type resolution and dynamic assembly loading

**Problem:** The trimmer cannot track `Assembly.GetType(string)` / `Type.GetType(string)`. Dynamic loading through `Assembly.LoadFile` / `LoadFrom` is unsupported on AOT.

**Mitigation:** generate static registration code into a `[ModuleInitializer]` with a Source Generator / apply `[RequiresUnreferencedCode]`. If a plugin mechanism is a hard requirement, accept that the API is AOT-incompatible and say so with the attribute.

### AOTP-08: Reflection-based serialization

**Problem:** The reflection mode of `JsonSerializer.Deserialize<T>(json)` throws `NotSupportedException` under AOT because the type information has been stripped.

**Mitigation:**

```csharp
// Define a JsonSerializerContext (the Source Generator emits the implementation)
[JsonSerializable(typeof(MyResponse))]
[JsonSerializable(typeof(MyRequest))]
internal partial class AppJsonContext : JsonSerializerContext
{
}

// At the call site
JsonSerializer.Deserialize(json, AppJsonContext.Default.MyResponse);
```

**Related caveats:**

- `JsonStringEnumConverter` (the non-generic version) is not AOT-compatible → use `JsonStringEnumConverter<TEnum>` from .NET 8+
- Serializing anonymous types does not preserve type information under AOT → use a concrete type, or `JsonNode` / `JsonObject`

### AOTP-09: Reflection-based configuration binding and DI

**Problem:**

- `IConfiguration.Bind<T>()` / `Configure<T>()` bind to properties through reflection, so trimming breaks them
- The same applies to reflection-based DI such as `ActivatorUtilities.CreateInstance`
- APIs that dynamically load type names out of a configuration file, like `Serilog.ReadFrom.Configuration()`, are fundamentally incompatible with AOT

**Mitigation:** the Configuration Binder Source Generator (.NET 8+) / standardize DI on generic registration (`AddTransient<T>()`) / a Source Generator based DI container (Jab, Pure.DI, and similar) / move configuration into code (a fluent API).

### AOTP-10: RegexOptions.Compiled / dynamic patterns

**Problem:** `RegexOptions.Compiled` throws `PlatformNotSupportedException` under AOT. Patterns assembled at runtime still work in interpreted mode, but slowly.

**Mitigation:**

```csharp
// Before
Regex.Replace(value, @"[%_\[]", "[$0]");

// After (.NET 7+ Source Generator)
[GeneratedRegex(@"[%_\[]")]
private static partial Regex LikeEscapePattern();
```

For dynamic patterns, consider replacing the regex with `string.StartsWith` / `char` checks, `SearchValues<T>`, and the like.

---

## 🛡️ Mitigation patterns (AOTS)

| ID | Pattern | Role | Primary use |
|---|---|---|---|
| [AOTS-01](#aots-01-source-generator-root-fix) | Source Generator | Root fix | Eliminating reflection and Emit |
| [AOTS-02](#aots-02-standardize-on-generic-apis) | Standardize on generic APIs | Root fix | Pinning types at compile time |
| [AOTS-03](#aots-03-factory-delegate-registration) | Factory delegate registration | Root fix | Turning dynamic creation into up-front registration |
| [AOTS-04](#aots-04-dynamicallyaccessedmembers) | \[DynamicallyAccessedMembers\] | Legitimate annotation | Telling the trimmer to keep reflection targets |
| [AOTS-05](#aots-05-dynamicdependency) | \[DynamicDependency\] | Supplementary | Keeping fixed dependency members |
| [AOTS-06](#aots-06-requiresunreferencedcode--requiresdynamiccode) | \[RequiresUnreferencedCode\] / \[RequiresDynamicCode\] | Declaring incompatibility | Propagating warnings to callers |
| [AOTS-07](#aots-07-unconditionalsuppressmessage-last-resort) | \[UnconditionalSuppressMessage\] | Last resort | Suppressing warnings already proven safe |
| [AOTS-08](#aots-08-dual-paths-via-runtimefeature) | Dual paths via RuntimeFeature | Compatibility strategy | Emit on JIT, fallback on AOT |
| [AOTS-09](#aots-09-rdxml--trimmerrootdescriptor-stopgap) | rd.xml / TrimmerRootDescriptor | Stopgap | Keeping types, mainly on the app side |
| [AOTS-10](#aots-10-illinksubstitutionsxml) | ILLink.Substitutions.xml | Supplementary | Freezing branches at trim time |
| [AOTS-11](#aots-11-project-settings-and-ci-verification) | Project settings and CI verification | Quality gate | Surfacing warnings and keeping them at zero |

### AOTS-01: Source Generator (root fix)

Replacing reflection and Emit with compile-time code generation is the **single most important technique** for AOT support.

- Use a Roslyn incremental source generator to emit static code driven by marker attributes
- Performance-wise you get zero runtime generation cost plus inlineable static code, so it can match or beat Emit
- Examples in the framework itself: `System.Text.Json` (JsonSerializerContext), `[GeneratedRegex]`, `[LibraryImport]`, the Configuration Binder

**Library design guidance:** as a rule, any feature that "works hard at runtime with reflection" should be redesigned to "generate at build time with a Source Generator". If you keep the runtime (reflection-based) API, isolate it behind `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]`.

**What to generate for speed:** [generated-code-patterns.md](generated-code-patterns.md) (GEN-02) collects the shapes of generated code per scenario (name switches, per-type specialization, row mappers, expanded factories, and so on) with the measurements backing them, plus an anti-generation list of things you should never generate.

### AOTS-02: Standardize on generic APIs

Taking the type as a compile-time type argument rather than a runtime `Type` lets the trimmer and the AOT compiler determine statically which code is required.

```csharp
// ❌ Type based: can break under trimming
public object Create(Type type) => Activator.CreateInstance(type)!;

// ✅ Generic + constraint: resolved at compile time, AOT safe
public T Create<T>() where T : new() => new T();

// ✅ Generic + attribute: when a new() constraint is not an option
public T Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
    => (T)Activator.CreateInstance(typeof(T))!;
```

### AOTS-03: Factory delegate registration

Replace dynamic instance creation with up-front registration by the consumer. The registration code is itself a static reference, so the trimmer keeps the target.

```csharp
public sealed class Registry
{
    private readonly Dictionary<Type, Func<object>> factories = new();

    public void Register<T>(Func<T> factory) where T : class
        => factories[typeof(T)] = factory;

    public T Resolve<T>() where T : class
        => (T)factories[typeof(T)]();
}

// Consumer side: new MyService() is referenced statically, so it survives trimming
registry.Register<MyService>(static () => new MyService());
```

### AOTS-04: \[DynamicallyAccessedMembers\]

An attribute that tells the trimmer "the type passed through this type parameter or argument needs members of the given categories". It is a **legitimate design technique for library authors** and propagates automatically along the call chain.

```csharp
// On a parameter
public object CreateInstance(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    => Activator.CreateInstance(type)!;

// On a generic type parameter
public T Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
    => (T)Activator.CreateInstance(typeof(T))!;
```

- The trimmer tracks the concrete types callers pass in and keeps the matching members
- The value is a flags enum, so `PublicConstructors` / `PublicProperties` / `PublicMethods` / `All` and others can be combined
- Its reach ends at types that can be tracked statically; it does nothing for runtime resolution such as `Type.GetType(userInput)` (use AOTS-06 there)

### AOTS-05: \[DynamicDependency\]

Statically declares "this method depends on a specific type or member" and keeps it unconditionally. Use it when the dependency is fixed.

```csharp
// Keep a fixed type that is referenced internally through reflection
[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(DefaultHandler))]
public void RegisterDefaults() { ... }

// Name a type in another assembly by string (works for third-party code too)
[DynamicDependency("Execute", "ThirdParty.SomeHandler", "ThirdPartyLib")]
public void Run() { ... }
```

**Choosing between this and AOTS-04:** if the caller decides the type (through a parameter), use `DynamicallyAccessedMembers`; if the target is fixed inside your own method, use `DynamicDependency`.

### AOTS-06: \[RequiresUnreferencedCode\] / \[RequiresDynamicCode\]

Apply to APIs that are inherently dynamic and cannot be fixed, propagating a warning (IL2026 / IL3050) to callers. It makes "this API is not AOT-compatible" an explicit part of the contract.

```csharp
[RequiresUnreferencedCode("Uses reflection to discover members. Use the generated accessor instead.")]
[RequiresDynamicCode("Uses MakeGenericType at runtime.")]
public object DynamicOperation(Type type) { ... }
```

| Attribute | Problem it covers | Warning | Related optimization |
|---|---|---|---|
| `RequiresUnreferencedCode` | Trimming removes code that is still needed | IL2026 | Trimming |
| `RequiresDynamicCode` | Runtime code generation is unavailable | IL3050 | Native AOT |

Apply both when both apply. The message should name the alternative API, so consumers can decide where to migrate.

### AOTS-07: \[UnconditionalSuppressMessage\] (last resort)

Suppresses a warning whose safety you have already guaranteed by other means. Unlike `#pragma` or a plain `[SuppressMessage]`, it is honored by trimmer and AOT analysis.

```csharp
[UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
    Justification = "All registered types are preserved via factory registration.")]
public void InitializeFromRegistry(string serviceName) { ... }
```

- Always spell out in `Justification` why it is safe
- Used without evidence it merely hides the cause of a runtime crash. Check first whether another attribute can handle the case

### AOTS-08: Dual paths via RuntimeFeature

A library compatibility strategy: take the fastest Emit / Expression path on JIT, and branch to a static fallback under AOT.

```csharp
public static Func<T> CreateFactory<[DynamicallyAccessedMembers(
    DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
    where T : new()
{
    if (RuntimeFeature.IsDynamicCodeCompiled)
    {
        return EmitFactoryBuilder.Build<T>(); // JIT: fastest path, via Emit
    }

    return static () => new T();              // AOT: static fallback
}
```

- `IsDynamicCodeSupported`: whether the dynamic code generation APIs are usable (false under Native AOT)
- `IsDynamicCodeCompiled`: whether the generated dynamic code actually gets compiled (also false in interpreted environments). **Use this one to gate the Emit fast path** — running Emit under the interpreter makes things slower, not faster
- The trimmer can constant-fold the `IsDynamicCodeSupported` branch in an AOT build and drop the unreachable Emit path

**Do you actually need dual paths? (measured):** in the GEN-01 measurements, the best Emit shape (holder field target, 6.55 ns) is on par with compiled code (6.23 ns), which means **straight-line code from a Source Generator reaches Emit-level performance while staying AOT safe** ([GEN-01-EmitStrategy.md](../benchmarks/results/GEN-01-EmitStrategy.md) / [generated-code-patterns.md](generated-code-patterns.md)). Dual paths only pay off for dynamic scenarios that cannot be generated at build time, such as composing types at runtime without touching consumer code.

### AOTS-09: rd.xml / TrimmerRootDescriptor (stopgap)

A mechanism for declaring "do not remove this type or member" in XML. It is **mainly a stopgap for application developers**; do not treat it as a library's real fix.

```xml
<!-- .csproj -->
<PropertyGroup>
  <TrimmerRootDescriptor>TrimmerRoots.xml</TrimmerRootDescriptor>
</PropertyGroup>
```

```xml
<!-- TrimmerRoots.xml -->
<Directives>
  <Application>
    <Assembly Name="MyApp">
      <Type Name="MyApp.Models.UserDto" Dynamic="Required All" />
    </Assembly>
    <!-- Types from AOT-incompatible third-party libraries can be kept too -->
    <Assembly Name="ThirdPartyLib" Dynamic="Required All" />
  </Application>
</Directives>
```

**When it fits:** a third-party library is not AOT-compatible and you cannot change its code / references that static analysis cannot follow, such as XAML / bridging the gap until a Source Generator is in place.

**When it does not:** fixing your own library (attributes handle that in a type-safe way) / keeping large numbers of types (the binary bloats and the AOT benefit evaporates).

### AOTS-10: ILLink.Substitutions.xml

Replaces a method's return value with a constant at trim time, removing the feature branch along with it.

```xml
<Substitutions>
  <Assembly Name="MyLib">
    <Type Name="MyLib.FeatureFlags">
      <Method Name="IsEnabled" Body="stub" Value="false" />
    </Type>
  </Assembly>
</Substitutions>
```

This is how you implement the "feature switch pattern": wrap the reflection-using code in a feature switch (`AppContext.TryGetSwitch`), pin it to false at trim time → the whole branch disappears.

### AOTS-11: Project settings and CI verification

**Library project:**

```xml
<PropertyGroup>
  <IsAotCompatible>true</IsAotCompatible>  <!-- Enables AOT/trimming warnings (net8.0+) -->
</PropertyGroup>
```

| Property | Declares trimming support | Declares AOT support | Warnings enabled |
|---|:---:|:---:|---|
| `IsTrimmable` only | ✅ | ❌ | IL2xxx |
| `IsAotCompatible` | ✅ (implied) | ✅ | IL2xxx + IL3xxx + single-file |

**Verification app (CI):**

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

```powershell
dotnet publish -r win-x64 -c Release -p:PublishAot=true
```

Only declare `IsAotCompatible` once you meet the bar: a warning-free build with `PublishAot=true`, plus execution tests run in an AOT environment.

---

## 🧭 Choosing between the attributes

```
A trimmer/AOT warning appeared
│
├─ Q: Can the reflected member be identified statically?
│   ├─ A specific member of a specific type → [DynamicDependency] (AOTS-05)
│   └─ A member category on a type passed as a parameter → [DynamicallyAccessedMembers] (AOTS-04)
│
├─ Q: Not covered by the above (inherently dynamic)?
│   ├─ Trimming-incompatible → propagate with [RequiresUnreferencedCode] (AOTS-06)
│   └─ AOT-incompatible → propagate with [RequiresDynamicCode] (apply both if both apply)
│
└─ Q: Is safety already guaranteed by other means?
    ├─ Yes → [UnconditionalSuppressMessage] + Justification (AOTS-07)
    └─ No → rework the design (consider moving to a Source Generator)
```

## 🤝 Division of responsibility: library authors and application developers

| Task | Library author | Application developer |
|---|:---:|:---:|
| Applying `[DynamicallyAccessedMembers]` | ✅ | ➖ |
| Applying `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]` | ✅ | ➖ |
| Applying `[DynamicDependency]` | ☑️ (fixed internal dependencies) | ☑️ (protecting types referenced from XAML/config) |
| `<IsAotCompatible>true</IsAotCompatible>` | ✅ | ➖ |
| `rd.xml` / `TrimmerRootDescriptor` | ⚠️ (avoid as a rule) | ✅ (dealing with third-party code) |
| Verifying with `PublishAot=true` | ☑️ (via the sample app in CI) | ✅ |

Legend: ✅ owns it / ☑️ assists / ⚠️ avoid as a rule / ➖ not applicable

## 🪜 Incremental adoption roadmap

| Phase | Details |
|---|---|
| 1. Surface the warnings | Add `<IsAotCompatible>true</IsAotCompatible>` and take stock of every build warning |
| 2. Apply the warning attributes | Declare AOT incompatibility with `[RequiresDynamicCode]` / `[RequiresUnreferencedCode]` |
| 3. Fill in `[DynamicallyAccessedMembers]` | Clear warnings by giving the trimmer hints |
| 4. Introduce Source Generators | Replace reflection and Emit with compile-time code generation |
| 5. AOT build tests | Add CI tests that build with `PublishAot=true` |
| 6. Keep warnings at zero | Require zero AOT warnings as a CI quality gate |

## 📦 Framework and major library status (reference)

> Accurate as of 2025. Check the current state before making adoption decisions.

**Frameworks:**

| Framework | Native AOT | Notes |
|---|:---:|---|
| Console app / Minimal API | ✅ | Officially supported (`PublishAot`) |
| ASP.NET Core MVC / Razor Pages | ❌ | Controller discovery relies on reflection → move to Minimal API |
| Blazor WASM | ✅ | `RunAOTCompilation=true` (AOT from IL to WASM; note that `Expression.Compile` is unavailable) |
| WPF / WinUI 3 | ❌ | XAML relies on reflection. `PublishReadyToRun` can still improve startup |
| .NET MAUI | ⚠️ | Full AOT is mandatory on iOS. Other platforms have limitations |
| Avalonia UI | ⚠️ | `CompiledBindings` can remove reflection; support is still in progress |

**Libraries confirmed AOT-incompatible, and alternatives:**

| Library | Problem | Alternative |
|---|---|---|
| CsvHelper | Maps through reflection | Sylvan.Data.Csv |
| Apache.Avro | Heavy reflection use | Chr.Avro |
| Swashbuckle | Generates schemas through reflection | Microsoft.AspNetCore.OpenApi |
| System.Reactive | Dynamic code in some operators | R3 |
| System.Management (WMI) | Heavy reflection and COM use | Direct Win32 API calls |

## ☑️ Checklist (when a library counts as AOT-ready)

- [ ] IsAotCompatible is set
- [ ] Built the verification app with PublishTrimmed=true / PublishAot=true
- [ ] Reviewed every IL2xxx / IL3xxx warning
- [ ] Addressed each warning in this order of preference:
      1. Fix at the root through a design change (generic API / Source Generator)
      2. Preserve with [DynamicallyAccessedMembers] / [DynamicDependency]
      3. Propagate with [RequiresUnreferencedCode] / [RequiresDynamicCode]
      4. Suppress with [UnconditionalSuppressMessage] (state the justification)
- [ ] Confirmed the final build has zero warnings
- [ ] Ran execution tests in an AOT environment (a PublishAot executable)
