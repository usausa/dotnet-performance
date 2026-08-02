# 🧩 AOT / トリミング対応パターン一覧

Native AOT・トリミング対応ライブラリを実装するためのパターンカタログ。

- **非互換パターン(AOTP)**: 避けるべき・注意すべき実装と、その代替手段
- **対策パターン(AOTS)**: ライブラリ設計側で使う対応手法
- パフォーマンス実装パターンの AOT 可否は [README](../README.md) のパターン一覧の AOT 列を参照

## 🗺️ 全体像

| 最適化 | 内容 | 壊れるもの | 警告 |
|---|---|---|---|
| トリミング(`PublishTrimmed`) | 未使用コードの削除 | リフレクションで暗黙参照されるメンバー | IL2xxx |
| Native AOT(`PublishAot`) | 事前ネイティブコンパイル(トリミングを包含) | 実行時コード生成 + 上記 | IL3xxx + IL2xxx |

**ライブラリの対応方針(推奨順):**

1. リフレクション・動的コード生成を使わない設計にする(ジェネリック API / Source Generator)
2. 使う場合は属性(`[DynamicallyAccessedMembers]` 等)でトリマーに情報を与える
3. 本質的に動的な API は `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]` で非互換を明示する

---

## 🚫 非互換パターン一覧(AOTP)

| ID | パターン | 深刻度 | 主な症状 | 主な代替 |
|---|---|:---:|---|---|
| [AOTP-01](#aotp-01-reflectionemit) | Reflection.Emit(DynamicMethod / ILGenerator 等) | 致命的 | `PlatformNotSupportedException` | Source Generator |
| [AOTP-02](#aotp-02-expressiontcompile) | Expression\<T\>.Compile() | 致命的 | 例外またはインタープリタ実行で激遅 | Source Generator / 直接 API |
| [AOTP-03](#aotp-03-activatorcreateinstancetype) | Activator.CreateInstance(Type) | 高 | トリミングで `MissingMethodException` | ジェネリック API + 属性 |
| [AOTP-04](#aotp-04-makegenerictype--makegenericmethod) | MakeGenericType / MakeGenericMethod | 高 | 値型の組み合わせで実行時エラー(IL3050) | Source Generator / 静的分岐 |
| [AOTP-05](#aotp-05-メタデータ走査) | GetProperties / GetMethods / GetCustomAttribute | 高〜中 | トリミングでメンバー欠落 | 属性 / Source Generator |
| [AOTP-06](#aotp-06-propertyinfosetvalue--methodinfoinvoke) | PropertyInfo.SetValue / MethodInfo.Invoke | 高〜中 | トリミングで実行時エラー(IL2026) | 生成アクセサ / デリゲート登録 |
| [AOTP-07](#aotp-07-文字列ベース型解決動的アセンブリロード) | Assembly.GetType(string) / Assembly.LoadFrom | 高 | トリマー追跡不可・AOT 非サポート | 静的登録(ModuleInitializer) |
| [AOTP-08](#aotp-08-リフレクションベースのシリアライズ) | System.Text.Json リフレクションモード等 | 高 | `NotSupportedException` | JsonSerializerContext(SG) |
| [AOTP-09](#aotp-09-リフレクションベースの設定バインドdi) | ConfigurationBinder / リフレクション DI | 中 | バインド失敗・解決失敗 | Binder SG / ジェネリック登録 |
| [AOTP-10](#aotp-10-regexoptionscompiled--動的パターン) | RegexOptions.Compiled / 動的パターン | 低〜中 | 例外または低速化 | \[GeneratedRegex\] |

### AOTP-01: Reflection.Emit

**問題:** `DynamicMethod` / `ILGenerator` / `TypeBuilder` / `AssemblyBuilder` による実行時 IL 生成は AOT で完全に非サポート。`PlatformNotSupportedException` が発生する。

**該当しやすい実装:** DI コンテナのファクトリ生成、O/R マッパーの動的マッパー、getter/setter デリゲートファクトリ。

**対策:**

| 方針 | 内容 |
|---|---|
| Source Generator(推奨) | Roslyn Incremental Source Generator でコンパイル時にコードを生成([AOTS-01](#aots-01-source-generator根本対策)) |
| リフレクションフォールバック | `PropertyInfo.GetValue/SetValue` ベースの低速版を AOT 用に提供 |
| 実行環境による分岐 | `RuntimeFeature.IsDynamicCodeSupported` で Emit パスを回避([AOTS-08](#aots-08-runtimefeature-による二重パス)) |
| `[RequiresDynamicCode]` 付与 | AOT 非対応を明示し、利用者にコンパイル時警告を出す([AOTS-06](#aots-06-requiresunreferencedcode--requiresdynamiccode)) |

### AOTP-02: Expression\<T\>.Compile()

**問題:** 式ツリーのランタイムコンパイルは AOT で機能しない。Native AOT ではインタープリタ実行にフォールバックして大幅に低速化し、IL インタープリタが無効な環境(Blazor WASM AOT 等)では例外が発生する。

**該当しやすい実装:** プロパティアクセサの動的生成、O/R マッパーのカラムマッピング、`FieldIdentifier.Create(expression)` 系 API。

**対策:** Source Generator によるアクセサ静的生成 / Expression を使わないオーバーロードの追加 / `[RequiresDynamicCode]` 付与。

### AOTP-03: Activator.CreateInstance(Type)

**問題:** トリミングによりコンストラクタが除去されると `MissingMethodException` が発生する。

**対策:**

| 方針 | 内容 |
|---|---|
| ジェネリック API への統一 | `Create<T>()` で型をコンパイル時に確定([AOTS-02](#aots-02-ジェネリック-api-への統一)) |
| ファクトリデリゲート登録 | `Register<T>(Func<T> factory)` パターン([AOTS-03](#aots-03-ファクトリデリゲート登録)) |
| `[DynamicallyAccessedMembers(PublicConstructors)]` | 型パラメータ・引数に付与しトリマーにヒントを提供([AOTS-04](#aots-04-dynamicallyaccessedmembers)) |

### AOTP-04: MakeGenericType / MakeGenericMethod

**問題:** コンパイル時に存在しないジェネリックインスタンス化は AOT で生成できない(IL3050)。参照型の型引数は共有実装(shared generics)で動く場合が多いが、**値型の型引数は事前生成されていない限り実行時エラー**になる。

**該当しやすい実装:** `typeof(Option<>).MakeGenericType(property.PropertyType)`、`method.MakeGenericMethod(targetType)`。

**対策:** Source Generator で使用される型の組み合わせをコンパイル時に列挙・生成 / 既知の型に対する静的ディスパッチ(switch 式)/ `[RequiresDynamicCode]` 付与。

### AOTP-05: メタデータ走査

**問題:** `Type.GetProperties()` / `GetMethods()` / `GetCustomAttribute<T>()` によるメタデータ走査は、トリミングにより対象メンバー・属性が除去されて空振りする。

**対策:** `[DynamicallyAccessedMembers]` でメタデータ保持を指示 / Source Generator でビルド時に属性・プロパティ情報を静的コードに埋め込み / `rd.xml`(暫定対応、[AOTS-09](#aots-09-rdxml--trimmerrootdescriptor暫定対応))。

### AOTP-06: PropertyInfo.SetValue / MethodInfo.Invoke

**問題:** トリミングでメンバーが除去されると実行時エラー。IL2026 警告の対象。

**対策:** Source Generator による静的アクセサ生成 / `Action<T, TValue>` 形式の事前デリゲート登録 / インターフェース契約への移行。対象が既知の非公開メンバーであれば `[UnsafeAccessor]`([README](../README.md#typ-03-unsafeaccessor非公開メンバーへの直接アクセス) の TYP-03)がリフレクション不要かつ AOT 互換の代替になる。

### AOTP-07: 文字列ベース型解決・動的アセンブリロード

**問題:** `Assembly.GetType(string)` / `Type.GetType(string)` はトリマーが追跡不可。`Assembly.LoadFile` / `LoadFrom` による動的ロードは AOT 非サポート。

**対策:** Source Generator で `[ModuleInitializer]` に静的登録コードを生成 / `[RequiresUnreferencedCode]` 付与。プラグイン機構が必須要件の場合、その API は AOT 非対応と割り切って属性で明示する。

### AOTP-08: リフレクションベースのシリアライズ

**問題:** `JsonSerializer.Deserialize<T>(json)` のリフレクションモードは AOT で型情報が削除され `NotSupportedException` が発生する。

**対策:**

```csharp
// JsonSerializerContext を定義(Source Generator が生成)
[JsonSerializable(typeof(MyResponse))]
[JsonSerializable(typeof(MyRequest))]
internal partial class AppJsonContext : JsonSerializerContext
{
}

// 使用時
JsonSerializer.Deserialize(json, AppJsonContext.Default.MyResponse);
```

**関連の注意点:**

- `JsonStringEnumConverter`(非ジェネリック版)は AOT 非対応 → .NET 8+ の `JsonStringEnumConverter<TEnum>` を使う
- 匿名型のシリアライズは AOT で型情報が保持されない → 具体型または `JsonNode` / `JsonObject` を使う

### AOTP-09: リフレクションベースの設定バインド・DI

**問題:**

- `IConfiguration.Bind<T>()` / `Configure<T>()` はリフレクションでプロパティにバインドするため、トリミングで壊れる
- `ActivatorUtilities.CreateInstance` 等のリフレクション DI も同様
- `Serilog.ReadFrom.Configuration()` のような「設定ファイルから型名を動的ロード」する API は AOT と根本的に非互換

**対策:** Configuration Binder Source Generator(.NET 8+)/ DI はジェネリック登録(`AddTransient<T>()`)に統一 / Source Generator ベースの DI コンテナ(Jab, Pure.DI 等)/ 設定はコードベース(Fluent API)に切り替える。

### AOTP-10: RegexOptions.Compiled / 動的パターン

**問題:** `RegexOptions.Compiled` は AOT で `PlatformNotSupportedException`。実行時に組み立てる動的パターンは Interpreted モードで動作するが低速。

**対策:**

```csharp
// Before
Regex.Replace(value, @"[%_\[]", "[$0]");

// After (.NET 7+ Source Generator)
[GeneratedRegex(@"[%_\[]")]
private static partial Regex LikeEscapePattern();
```

動的パターンの場合は `string.StartsWith` / `char` 判定、`SearchValues<T>` 等での代替を検討する。

---

## 🛡️ 対策パターン一覧(AOTS)

| ID | パターン | 位置づけ | 主な用途 |
|---|---|---|---|
| [AOTS-01](#aots-01-source-generator根本対策) | Source Generator | 根本対策 | リフレクション・Emit の排除 |
| [AOTS-02](#aots-02-ジェネリック-api-への統一) | ジェネリック API への統一 | 根本対策 | 型をコンパイル時に確定させる |
| [AOTS-03](#aots-03-ファクトリデリゲート登録) | ファクトリデリゲート登録 | 根本対策 | 動的生成の事前登録化 |
| [AOTS-04](#aots-04-dynamicallyaccessedmembers) | \[DynamicallyAccessedMembers\] | 正当な注釈 | リフレクション対象の保持指示 |
| [AOTS-05](#aots-05-dynamicdependency) | \[DynamicDependency\] | 補完策 | 固定依存メンバーの保持 |
| [AOTS-06](#aots-06-requiresunreferencedcode--requiresdynamiccode) | \[RequiresUnreferencedCode\] / \[RequiresDynamicCode\] | 非互換の明示 | 警告の呼び出し元への伝播 |
| [AOTS-07](#aots-07-unconditionalsuppressmessage最終手段) | \[UnconditionalSuppressMessage\] | 最終手段 | 安全性保証済み警告の抑制 |
| [AOTS-08](#aots-08-runtimefeature-による二重パス) | RuntimeFeature による二重パス | 互換戦略 | JIT では Emit、AOT ではフォールバック |
| [AOTS-09](#aots-09-rdxml--trimmerrootdescriptor暫定対応) | rd.xml / TrimmerRootDescriptor | 暫定対応 | 主にアプリ側での型保持 |
| [AOTS-10](#aots-10-illinksubstitutionsxml) | ILLink.Substitutions.xml | 補完策 | トリミング時の分岐固定化 |
| [AOTS-11](#aots-11-プロジェクト設定と-ci-検証) | プロジェクト設定と CI 検証 | 品質ゲート | 警告の可視化と維持 |

### AOTS-01: Source Generator(根本対策)

リフレクション・Emit をコンパイル時コード生成に置き換える、AOT 対応の**最重要手段**。

- Roslyn Incremental Source Generator で、属性等をマークにした静的コードを生成する
- パフォーマンス面でも「実行時生成コストゼロ + インライン化可能な静的コード」となり、Emit と同等以上に達しうる
- 公式の適用例: `System.Text.Json`(JsonSerializerContext)、`[GeneratedRegex]`、`[LibraryImport]`、Configuration Binder

**ライブラリ設計への指針:** 「リフレクションで実行時に頑張る」機能は「Source Generator でビルド時に生成する」形に設計し直すのが原則。実行時 API(リフレクション版)を残す場合は `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]` を付けて分離する。

**何を生成すれば速いか:** シナリオ別の生成コード形(名前スイッチ・型別焼き込み・行マッパー・ファクトリ展開など)と実測根拠、および生成してはいけないアンチ生成リストを [generated-code-patterns.md](generated-code-patterns.md)(GEN-02)にまとめている。

### AOTS-02: ジェネリック API への統一

型を実行時の `Type` ではなくコンパイル時の型引数で受けることで、トリマー・AOT コンパイラが必要コードを静的に確定できる。

```csharp
// ❌ Type ベース: トリミングで壊れうる
public object Create(Type type) => Activator.CreateInstance(type)!;

// ✅ ジェネリック + 制約: コンパイル時に確定、AOT 安全
public T Create<T>() where T : new() => new T();

// ✅ ジェネリック + 属性: new() 制約が使えない場合
public T Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
    => (T)Activator.CreateInstance(typeof(T))!;
```

### AOTS-03: ファクトリデリゲート登録

動的なインスタンス生成を「利用者による事前登録」に置き換える。登録コード自体が静的参照になるため、トリマーは対象を除去しない。

```csharp
public sealed class Registry
{
    private readonly Dictionary<Type, Func<object>> factories = new();

    public void Register<T>(Func<T> factory) where T : class
        => factories[typeof(T)] = factory;

    public T Resolve<T>() where T : class
        => (T)factories[typeof(T)]();
}

// 利用者側: new MyService() が静的に参照されるためトリミングされない
registry.Register<MyService>(static () => new MyService());
```

### AOTS-04: \[DynamicallyAccessedMembers\]

「この型パラメータ/引数で渡される型は、指定カテゴリのメンバーが必要」とトリマーに伝える属性。**ライブラリ作者が付与する正当な設計手法**であり、呼び出しチェーンを通じて自動伝播する。

```csharp
// パラメータに付ける
public object CreateInstance(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    => Activator.CreateInstance(type)!;

// ジェネリック型パラメータに付ける
public T Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
    => (T)Activator.CreateInstance(typeof(T))!;
```

- トリマーは呼び出し元で渡される具体型を追跡し、該当メンバーを保持する
- 指定値は `PublicConstructors` / `PublicProperties` / `PublicMethods` / `All` 等のフラグ列挙で組み合わせ可能
- 効果範囲は「静的に追跡できる型」まで。`Type.GetType(userInput)` のような実行時解決には効かない(その場合は AOTS-06)

### AOTS-05: \[DynamicDependency\]

「このメソッドは特定の型・メンバーに依存している」と静的に宣言し、無条件に保持させる。依存先が固定の場合に使う。

```csharp
// 内部でリフレクション参照する固定型を保持
[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(DefaultHandler))]
public void RegisterDefaults() { ... }

// 外部アセンブリの型を文字列で指定(サードパーティにも使える)
[DynamicDependency("Execute", "ThirdParty.SomeHandler", "ThirdPartyLib")]
public void Run() { ... }
```

**AOTS-04 との使い分け:** 呼び出し元が型を決める(パラメータ経由)なら `DynamicallyAccessedMembers`、自メソッド内で対象が固定なら `DynamicDependency`。

### AOTS-06: \[RequiresUnreferencedCode\] / \[RequiresDynamicCode\]

本質的に動的で対応不可能な API に付与し、呼び出し元へ警告(IL2026 / IL3050)を伝播させる。「この API は AOT 非対応」という契約の明示。

```csharp
[RequiresUnreferencedCode("Uses reflection to discover members. Use the generated accessor instead.")]
[RequiresDynamicCode("Uses MakeGenericType at runtime.")]
public object DynamicOperation(Type type) { ... }
```

| 属性 | 対象の問題 | 警告 | 関連する最適化 |
|---|---|---|---|
| `RequiresUnreferencedCode` | トリミングで必要コードが削除される | IL2026 | トリミング |
| `RequiresDynamicCode` | 実行時コード生成ができない | IL3050 | Native AOT |

両方該当する場合は両方付ける。推奨メッセージには「代替 API」を含める(利用者が移行先を判断できる)。

### AOTS-07: \[UnconditionalSuppressMessage\](最終手段)

別の手段で安全性を保証済みの警告を抑制する。`#pragma` や通常の `[SuppressMessage]` と異なり、トリマー/AOT 解析に対して有効。

```csharp
[UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
    Justification = "All registered types are preserved via factory registration.")]
public void InitializeFromRegistry(string serviceName) { ... }
```

- 必ず `Justification` に「なぜ安全なのか」を記述する
- 根拠なく使うと実行時クラッシュの原因を隠すだけになる。他の属性で対処できないか先に検討する

### AOTS-08: RuntimeFeature による二重パス

JIT 環境では Emit / Expression の最速パス、AOT 環境では静的フォールバックに分岐するライブラリ互換戦略。

```csharp
public static Func<T> CreateFactory<[DynamicallyAccessedMembers(
    DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
    where T : new()
{
    if (RuntimeFeature.IsDynamicCodeCompiled)
    {
        return EmitFactoryBuilder.Build<T>(); // JIT 環境: Emit による最速パス
    }

    return static () => new T();              // AOT 環境: 静的フォールバック
}
```

- `IsDynamicCodeSupported`: 動的コード生成 API が使えるか(Native AOT で false)
- `IsDynamicCodeCompiled`: 生成した動的コードがコンパイルされるか(インタープリタ実行環境でも false)。**Emit 高速パスの判定にはこちらを使う**(インタープリタで Emit を走らせると逆に遅くなるため)
- トリマーは `IsDynamicCodeSupported` の分岐を AOT ビルド時に定数畳み込みし、到達不能な Emit パスを削除できる

**二重パスが本当に必要かの判断(実測):** GEN-01 の測定では、Emit の最良形(Holder フィールドターゲット 6.55 ns)はコンパイル済みコード(6.23 ns)と同等であり、**Source Generator の直書き生成コードは Emit 側と同等性能を AOT 安全に出せる**([GEN-01-EmitStrategy.md](../benchmarks/results/GEN-01-EmitStrategy.md) / [generated-code-patterns.md](generated-code-patterns.md))。二重パスを組む価値があるのは「ビルド時に生成できない動的シナリオ」(利用者コードに触れない実行時型合成など)に限られる。

### AOTS-09: rd.xml / TrimmerRootDescriptor(暫定対応)

「この型・メンバーを削除しないでください」を XML で宣言する仕組み。**主にアプリ開発者側の暫定策**であり、ライブラリの根本対応としては使わない。

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
    <!-- サードパーティの AOT 非対応ライブラリの型も保持可能 -->
    <Assembly Name="ThirdPartyLib" Dynamic="Required All" />
  </Application>
</Directives>
```

**適切な場面:** サードパーティが AOT 非対応でコード変更できない / XAML 等の静的解析困難な参照 / Source Generator 導入までのつなぎ。

**不適切な場面:** 自作ライブラリの対応(属性で型安全に対応可能)/ 大量の型保持(バイナリ肥大化で AOT の恩恵消失)。

### AOTS-10: ILLink.Substitutions.xml

トリミング時にメソッドの戻り値を固定値へ置き換え、フィーチャー分岐ごと除去する。

```xml
<Substitutions>
  <Assembly Name="MyLib">
    <Type Name="MyLib.FeatureFlags">
      <Method Name="IsEnabled" Body="stub" Value="false" />
    </Type>
  </Assembly>
</Substitutions>
```

リフレクション使用部をフィーチャースイッチ(`AppContext.TryGetSwitch`)で括り、トリミング時に false 固定 → 分岐ごと削除、という「フィーチャースイッチパターン」の実現手段。

### AOTS-11: プロジェクト設定と CI 検証

**ライブラリプロジェクト:**

```xml
<PropertyGroup>
  <IsAotCompatible>true</IsAotCompatible>  <!-- AOT/トリミング警告を有効化(net8.0+) -->
</PropertyGroup>
```

| プロパティ | トリミング対応宣言 | AOT 対応宣言 | 有効になる警告 |
|---|:---:|:---:|---|
| `IsTrimmable` のみ | ✅ | ❌ | IL2xxx |
| `IsAotCompatible` | ✅(暗黙的に含む) | ✅ | IL2xxx + IL3xxx + 単一ファイル |

**検証用アプリ(CI):**

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

```powershell
dotnet publish -r win-x64 -c Release -p:PublishAot=true
```

`IsAotCompatible` の宣言は「`PublishAot=true` でビルドして警告ゼロ + AOT 環境での実行テスト済み」を満たしてから行う。

---

## 🧭 属性の使い分けフロー

```
トリマー/AOT 警告が出た
│
├─ Q: リフレクション対象のメンバーが静的に特定できるか?
│   ├─ 特定の型の特定メンバー → [DynamicDependency] (AOTS-05)
│   └─ パラメータで渡される型のメンバーカテゴリ → [DynamicallyAccessedMembers] (AOTS-04)
│
├─ Q: 上記で対処できない(本質的に動的)か?
│   ├─ トリミング非互換 → [RequiresUnreferencedCode] で伝播 (AOTS-06)
│   └─ AOT 非互換 → [RequiresDynamicCode] で伝播(両方なら両方付ける)
│
└─ Q: 別の手段で安全性を保証済みか?
    ├─ Yes → [UnconditionalSuppressMessage] + Justification (AOTS-07)
    └─ No → コードの設計を見直す(Source Generator 化を検討)
```

## 🤝 責任分担:ライブラリ作者とアプリ開発者

| 対応 | ライブラリ作者 | アプリ開発者 |
|---|:---:|:---:|
| `[DynamicallyAccessedMembers]` の付与 | ◎ 主担当 | − |
| `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]` の付与 | ◎ 主担当 | − |
| `[DynamicDependency]` の付与 | ○(内部の固定依存) | ○(XAML/設定参照型の保護) |
| `<IsAotCompatible>true</IsAotCompatible>` | ◎ 主担当 | − |
| `rd.xml` / `TrimmerRootDescriptor` | △(原則使わない) | ◎(サードパーティ対応) |
| `PublishAot=true` での検証 | ○(CI のサンプルアプリで) | ◎ 最終確認 |

## 🪜 段階的対応ロードマップ

| フェーズ | 内容 |
|---|---|
| 1. 警告の可視化 | `<IsAotCompatible>true</IsAotCompatible>` を追加し、ビルド警告を全量把握 |
| 2. 警告属性の付与 | `[RequiresDynamicCode]` / `[RequiresUnreferencedCode]` で AOT 非対応を明示 |
| 3. `[DynamicallyAccessedMembers]` 整備 | トリマーへのヒント付与で警告を解消 |
| 4. Source Generator 導入 | リフレクション・Emit をコンパイル時コード生成に置換 |
| 5. AOT ビルドテスト | `PublishAot=true` での CI テストを追加 |
| 6. 警告ゼロの維持 | CI の品質ゲートとして AOT 警告ゼロを要求 |

## 📦 フレームワーク・主要ライブラリの対応状況(参考)

> 2025 年時点の情報。採用判断時は最新状況を確認すること。

**フレームワーク:**

| フレームワーク | Native AOT | 備考 |
|---|:---:|---|
| コンソールアプリ / Minimal API | ✅ | 公式対応(`PublishAot`) |
| ASP.NET Core MVC / Razor Pages | ❌ | コントローラー自動検出がリフレクション依存 → Minimal API へ移行 |
| Blazor WASM | ✅ | `RunAOTCompilation=true`(IL→WASM の AOT、`Expression.Compile` 不可に注意) |
| WPF / WinUI 3 | ❌ | XAML がリフレクション依存。`PublishReadyToRun` で起動改善は可能 |
| .NET MAUI | ⚠️ | iOS は Full AOT 必須。他プラットフォームは制限あり |
| Avalonia UI | ⚠️ | `CompiledBindings` でリフレクション排除可能、対応進行中 |

**AOT 非対応が確認されたライブラリと代替の例:**

| ライブラリ | 問題 | 代替 |
|---|---|---|
| CsvHelper | リフレクションでマッピング | Sylvan.Data.Csv |
| Apache.Avro | リフレクション多用 | Chr.Avro |
| Swashbuckle | リフレクションでスキーマ生成 | Microsoft.AspNetCore.OpenApi |
| System.Reactive | 一部オペレータで動的コード | R3 |
| System.Management (WMI) | リフレクション・COM 多用 | Win32 API 直接呼び出し |

## ☑️ チェックリスト(ライブラリの AOT 対応完了条件)

```
□ IsAotCompatible を設定した
□ PublishTrimmed=true / PublishAot=true の検証アプリでビルドした
□ すべての IL2xxx / IL3xxx 警告を確認した
□ 各警告に対して以下の優先順位で対処した:
  1. 設計変更(ジェネリック API / Source Generator)で根本解決
  2. [DynamicallyAccessedMembers] / [DynamicDependency] で保持
  3. [RequiresUnreferencedCode] / [RequiresDynamicCode] で伝播
  4. [UnconditionalSuppressMessage] で抑制(根拠を明記)
□ 最終ビルドで警告がゼロであることを確認した
□ AOT 環境(PublishAot した実行ファイル)で実行テストを行った
```
