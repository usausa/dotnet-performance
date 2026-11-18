# B550H への引き継ぎ — Zen 3 での `RuntimeTypeHandle` キー退行の原因特定(2026-09-21)

HX 370(基準環境)での本測定と文書確定は完了している。残っているのは **B550H(Ryzen 9 5900X / Zen 3)でしか再現しない現象の原因特定**と、Survey への転記。完了後はこのファイルを削除してよい。

## 前提

- HX 370 側の変更は usausa がコミット / プッシュ済みであること(dotnet-performance と Generator2-DependencyInjection の両方)。B550H では最初に両リポジトリを `git pull` する
- `D:\GitHub\Performance-Applicability-Survey.md` は B550H にしかない(git 管理外)。HX 370 の結果は本書の付録 A に埋めてあるので、そこから貼る
- B550H の `BenchmarkDotNet.Artifacts/results/` に前回(仮測定時)の `*-asm.md` が残っていれば、実験 1 は再実行なしで済む

## そのまま貼れるプロンプト

```
dotnet-performance リポジトリで、docs/verification-handoff.ja.md の手順に従って B550H(Zen 3)側の作業を行なってください。

背景: HX 370(Zen 5)で本測定した結果、ConcurrentDictionary の RuntimeTypeHandle キーが B550H でだけ
「探索結果のデリゲートを 5 型ローテーションで呼ぶ」形で 2.24 倍遅くなる現象(TYP-07 補遺)は Zen 5 では再現せず
(0.82 倍)、原因は「デリゲート呼び出し先の PGO ガード + 間接呼び出しに対する Zen 3 の分岐予測」という仮説のまま
文書化されています。B550H でこの仮説を確定または反証してください。

やること:
1. git pull(dotnet-performance と Generator2-DependencyInjection)後、benchmarks/PerformancePatterns.Benchmarks で
   `dotnet run -c Release -f net10.0 -- --list flat` が通ること(Verify 群)を確認
2. 手順書「実験」の 1〜3 を実行(4 と 5 は任意)
3. 手順書「判定表」に従い、仮説を確定 / 修正 / 反証し、「差し替え箇所」の文言を書き換える
   (「〜と見られ」「Hypothesis, not yet confirmed」が残らないようにする)
4. benchmarks/results/TYP-07-TypeKey.md に「Run 4b (B550H): root cause」節を追加し、B550H の表を貼る(Run 4a は HX 370 のスイープ)
5. 付録 A を Survey の「🔬 ServiceRegistry」節へ貼り、実験 5 を行なった場合は B550H の再測定値を表に足す
6. README.ja.md / README.md と docs/benchmark-methodology.ja.md / .md の行数・見出し行番号が一致、
   全ファイル CRLF(TYP-07-TypeHashSource.md だけは既存が LF なので LF のまま)、ソリューションが 0 警告
7. 完了報告は「仮説が確定したか / 反証されたか」から書く

ルール: コミットしない(usausa が行う)。AGENTS.md(警告 0、警告抑制は要確認、改行コードを変えない)。
ベンチ実行中は同じマシンでビルドや他の重い処理をしない。ライブラリ本体(ServiceRegistry.cs)は計測後に必ず
git checkout で戻す。
```

## 実験

### 1. 既存 asm の確認(再実行不要なら 0 分)

`benchmarks/PerformancePatterns.Benchmarks/BenchmarkDotNet.Artifacts/results/PerformancePatterns.Benchmarks.Lab.TypeKeyDispatchBenchmark-asm.md` の `HandleRotating()` 本体に、次があるかを見る。

- `mov rcx,offset MT_...TypeKeyDispatchBenchmark+TargetN` の直後に `CORINFO_HELP_NEWSFAST`(= デリゲート呼び出し先の GDV ガード + `new` のインライン化)
- `call 00007FF...`(`TryGetHashCode` FCall の直接呼び出し)と `GetHashCodeWorker` フォールバック

HX 370 では `HandleRotating` 759 B / `PtrRotating` 480 B にこのガードがあり、B550H の仮測定では 669 B / 409 B だった(`TypeRotating` 548 B・比較子版 470 B・Single 版 589 / 1,168 B は両機同一)。ファイルが無ければ `--filter "*TypeKeyDispatchBenchmark*"`(約 4 分)で作る。

### 2. ターゲット数スイープ(約 8 分)

```
cd benchmarks/PerformancePatterns.Benchmarks
dotnet run -c Release -f net10.0 -- --filter "*TypeKeyDispatchSweepBenchmark*"
```

5 回呼び出しの列に含まれる**異なる型の数** `TargetCount` を 1 / 2 / 3 / 5 で変える(1 = Single 相当、5 = Rotating 相当)。Zen 3 で `HandleRotating` の Ratio が 1.0 を超える最小の `TargetCount` を記録する。HX 370 の基準値は下表。

### 3. 動的 PGO 無効(約 8 分)

```
dotnet run -c Release -f net10.0 -- --filter "*TypeKeyDispatchSweepBenchmark*" --envVars DOTNET_TieredPGO:0
```

`--envVars` は BDN の子プロセスに環境変数を渡すオプション。PGO が無いとデリゲート呼び出し先の推測(ガード)が生成されない。仮測定時に試した `DOTNET_JitEnableGuardedDevirtualization=0` では +8.7 ns → +3 ns に縮んだ(消えなかった)ので、`TieredPGO=0` で完全に消えるかどうかが分かれ目。

### 4. 分岐ミス予測カウンタ(任意、管理者権限が必要)

`PerformancePatterns.Benchmarks.csproj` に `BenchmarkDotNet.Diagnostics.Windows`(BDN と同じ 0.15.8)を一時的に足し、管理者のターミナルで

```
dotnet run -c Release -f net10.0 -- --filter "*TypeKeyDispatchBenchmark*Rotating*" --counters BranchMispredictions,BranchInstructions
```

1 op あたりのミス数を `TypeRotating` / `HandleRotating` / `PtrRotating` で比べる。Handle だけ数が多ければ仮説の直接の証拠になる。終わったらパッケージ参照は戻す(コミットしない)。

### 5. ServiceRegistry 実コード(任意、JIT 各約 3.5 分)

Generator2-DependencyInjection の `BunnyTail.DependencyInjection.Benchmark` で、HX 370 で作った `ActivateBenchmark`(pull で入る)を A / B / C の順に測る。

```
cd BunnyTail.DependencyInjection.Benchmark
dotnet run -c Release -- --filter "*ActivateBenchmark*" "*BunnyTailBenchmark.Transient"
```

B / C は付録 B のとおり `BunnyTail.DependencyInjection/Internal/ServiceRegistry.cs` を書き換えてから測り、終わったら `git checkout -- BunnyTail.DependencyInjection/Internal/ServiceRegistry.cs`。AOT は `BUNNYTAIL_BENCH_AOT_ONLY=1 … --runtimes nativeaot10.0`(PowerShell では `$env:BUNNYTAIL_BENCH_AOT_ONLY = "1"`)、`vswhere.exe` を PATH に通す(`C:\Program Files (x86)\Microsoft Visual Studio\Installer`)。

## 判定表

| 観測(B550H) | 解釈 | 文言 |
|---|---|---|
| 実験 3 で Handle の逆転が消える(Type 比 ≈ 0.8 倍) | デリゲート呼び出し先の GDV ガード × Zen 3 の分岐予測 → **仮説確定** | 「〜と見られ」を「〜による(PGO 無効で消える)」に |
| 実験 3 でも 2 倍前後のまま | ガードではなく間接呼び出し(または FCall)自体の予測失敗 → 仮説の主因を修正 | 「ガード + 間接呼び出し」→「間接呼び出し(PGO 無効でも残る)」に |
| 実験 2 で `TargetCount = 2` から逆転 | 当たり率 50% でも起きる = 履歴長ではなくガード / 間接呼び出しそのもの | 履歴長の記述を外す |
| 実験 2 で 4〜5 からのみ逆転 | 周期パターンの学習限界(履歴長依存) | 履歴長依存を明記 |
| 実験 1 で Handle 版にデリゲートガードが無い | 669 B の差はガード以外 → 「ガード」を仮説から外し、間接呼び出し + FCall の組み合わせとして書き直す | 同上 |
| 実験 2 / 3 で B550H でも逆転が再現しない | 仮測定時の 2.24 倍は配置依存の一過性(二峰性の片側)→ **反証**。README の Zen 3 記述を「B550H の仮測定 1 回で観測、再現せず」に落とす | 逆転を一般化しない |

## HX 370 基準値(スイープ、JIT、ns / 1 呼び出し)

| TargetCount | `Type` キー | `RuntimeTypeHandle` キー | `IntPtr` キー |
|---:|---:|---:|---:|
| 1 | 3.61 ns | 3.18 ns(0.88) | 2.60 ns(0.72) |
| 2 | 4.09 ns | 3.17 ns(0.77) | 2.68 ns(0.66) |
| 3 | 4.53 ns | 3.80 ns(0.84) | 2.90 ns(0.64) |
| 5 | 4.78 ns | 4.00 ns(0.84) | 3.23 ns(0.68) |

Zen 5 ではどの型数でも逆転しない(信頼区間はすべて非重複)。B550H の仮測定は 1 型で Handle 0.62 倍(4.33 vs 5.70 ns)、5 型で 2.24 倍(15.64 vs 6.98 ns)。表の全文は `benchmarks/results/TYP-07-TypeKey.md` の Run 4a。

## 差し替え箇所

| ファイル | 箇所 |
|---|---|
| `README.ja.md` / `README.md`(同じ行番号) | TYP-07 補遺「探索の後に結果でディスパッチする形の逆転は Zen 3 固有」段落の末尾(「…分岐予測側の効果と見られ、コアを跨いで一般化できない」) |
| `docs/benchmark-methodology.ja.md` / `.md` | 判定表 ⑧ 行(「差はデリゲート呼び出し先の PGO 推測」) |
| `benchmarks/results/TYP-07-TypeKey.md` | 先頭 bullet の「Hypothesis, not yet confirmed: …」以降を確定文言へ。「Run 4b (B550H): root cause」節を追加(環境ヘッダ付きの表 + 判定。Run 4a の HX 370 スイープの後) |
| Survey(B550H のみ) | 「🔬 ServiceRegistry」節に付録 A を貼り、実験 5 の値を表へ追記 |

## 完了条件

- `grep -rn "見られ\|not yet confirmed\|Hypothesis" README.ja.md README.md docs/benchmark-methodology.*.md benchmarks/results/TYP-07-TypeKey.md` で該当段落にヒットしない
- ja / en の行数と `#` 見出しの行番号が一致(README・benchmark-methodology)
- 変更ファイルは既存の改行コードのまま(CRLF。`TYP-07-TypeHashSource.md` のみ LF)
- `dotnet build dotnet-performance.slnx -c Release` が 0 警告、`ServiceRegistry.cs` が A(現行)に戻っている

## 付録 A: Survey 差し込み節(HX 370 の結果)

### 🔬 ServiceRegistry — HX 370(Ryzen AI 9 HX 370 / Zen 5、x86-64-v4)追記 2026-09-21

`Performance-Applicability-Survey.md` の「🔬 ServiceRegistry」節(1291 行付近)へ差し込む節。B550H(Zen 3)の数値は既存節のものを参照用に併記。

#### 計測条件

- `BunnyTail.DependencyInjection.Benchmark` の `ActivateBenchmark`(このマシンには B550H 側の同名ベンチが無かったため同構成で新規作成、未コミット): `ITypeActivator`(`provider.GetRequiredService<ITypeActivator>()`)経由で `Activate(Type)` を 5 型ローテーション(ActivateType)/ 同一型 5 回(ActivateTypeSingle)/ `Activate<T>()` 5 型(ActivateGeneric)。対象は未登録の空クラス `Activated1..5`(24 B)。`Activate(Type)` の引数は `Type[]` から渡し(CA2263 回避)、ジェネリック側の呼び出しで生成ファクトリが作られることを Setup で `CreateFactoryReport()` により検証(全型 `Generated`)
- D の目安 `Transient` は既存の `BunnyTailBenchmark.Transient`(`IServiceProvider.GetService(typeof(ITransientN))` ×5、`FixedTypeServiceTable` 経路)を同一プロセスで同時計測
- JIT: MediumRun(BenchmarkConfig)。AOT: `BUNNYTAIL_BENCH_AOT_ONLY=1 … --runtimes nativeaot10.0`(既定ジョブ)。.NET 10.0.12、BenchmarkDotNet 0.15.8
- A = 現行(`ConcurrentDictionary<Type, ServiceAccessor>` 既定比較子)、B = `RuntimeTypeHandle` キー(フィールド型 + `Activate` + `CreateFactoryReport` の 5 行)、C = `Type` キー + `TypeHandle.Value >> 4` 比較子、D = `FixedTypeServiceTable` の copy-on-write(未実装、`Transient` が目安)

#### 結果(ns / 1 呼び出し、全行 24 B)

| 案 | JIT ActivateType | JIT Single | JIT Generic | AOT ActivateType | AOT Single | AOT Generic |
|---|---:|---:|---:|---:|---:|---:|
| **A 現行** | 6.29 | 5.44 | 8.84 | 10.69 | 10.75 | 16.29 |
| **B `RuntimeTypeHandle` キー** | **5.19(0.83)** | 4.69(0.86) | 7.91(0.90) | **9.03(0.84)** | 8.95(0.83) | 14.55(0.89) |
| **C `Type` + `TypeHandle.Value` 比較子** | 5.70(0.91) | 5.22(0.96) | 8.46(0.96) | **11.74(1.10)** | 11.61(1.08) | 17.06(1.05) |
| D 目安(`Transient`、同一プロセス) | 4.46〜4.54 | — | — | 5.48〜5.51 | — | — |

括弧は A 比。JIT 側の信頼区間は A / B / C とも非重複(ActivateType: A 6.05〜6.47、B 5.11〜5.26、C 5.46〜5.84)。AOT 側も非重複(A 10.60〜10.84、B 8.94〜9.13、C 11.61〜11.84)。

B550H(Zen 3、JIT)の既存値: A 9.79 / 7.88 / 10.97、B **18.39**(1.88 倍退行)/ 6.86、C 7.82 / 6.71、Transient 7.21。

#### 読み方

1. **B の Zen 3 退行は Zen 5 では起きない。** 実コードで 0.83 倍、マイクロベンチ(`TypeKeyDispatchBenchmark`、dotnet-performance)でも 5 型ローテーション 0.82 倍・単一型 0.57 倍。探索部分の命令列は両機で同じ(Handle 版は `TryGetHashCode` FCall の直接呼び出し + `cmp` 1 本)で、差はデリゲート呼び出し先の PGO 推測(HX 370 の Handle / IntPtr 版は 1 ターゲットのガード + `new` インライン化、669 → 759 B / 409 → 480 B)。Zen 3 の 2.24 倍はこのガード + 間接呼び出しまわりの分岐予測側の効果と見られる(仮説、未確定 — B550H で `DOTNET_TieredPGO=0`・型数スイープ・分岐ミス予測カウンタを取れば確定できる)。コアを選べないライブラリでは Zen 3 の結果を捨てられない
2. **C は AOT で退行する。** 比較子へのインターフェース呼び出しを AOT が devirt しないため、探索単体でも 0.94 / 1.03 倍で効かず、ディスパッチ形では 1.11 倍(実コード 1.10 倍)。JIT では B550H 0.80 倍 / HX 370 0.91 倍で、Zen 3 で唯一退行しなかった案
3. **B は AOT で最も効く。** `Dictionary<RuntimeTypeHandle>` 探索単体で hit 0.50 倍、ディスパッチ形 0.79 倍、実コード 0.84 倍。AOT の `ConcurrentDictionary<Type>` 経路は JIT の 2 倍遅い(A 6.29 → 10.69)ため、AOT を出荷するなら A のままが最も不利
4. **D の目安(`Transient`)は JIT 4.5 / AOT 5.5 ns** — A に対して JIT 0.71 倍 / AOT 0.51 倍、B に対しても JIT 0.86 / AOT 0.61 倍。AOT で最大の差が出るのは D
5. `IntPtr`(`TypeHandle.Value`)キーはマイクロベンチでは全条件で勝つ(Zen 3 JIT 0.69 / Zen 5 JIT 0.64 / AOT 0.78)が、表が `Type` を参照しなくなるため collectible AssemblyLoadContext との相性を確認する必要があり、実コードでは未計測

#### 選択肢(判断は保留)

| 案 | JIT Zen 5 | JIT Zen 3 | AOT | 変更量 |
|---|---|---|---|---|
| A 現行維持 | 基準 | 基準 | 基準(最も遅い) | 0 |
| B `RuntimeTypeHandle` キー | 0.83 | **1.88(退行)** | 0.84 | 5 行 |
| C `TypeHandle.Value` 比較子 | 0.91 | 0.80 | **1.10(退行)** | 1 行 + 比較子クラス |
| D `FixedTypeServiceTable` COW | ≈0.71(目安) | ≈0.74(目安) | ≈0.51(目安) | 実装が必要 |
| (参考)`IntPtr` キー | 0.64(マイクロ) | 0.69(マイクロ) | 0.78(マイクロ) | 5 行 + ALC 要確認 |

計測後、`ServiceRegistry.cs` は `git checkout` で A に戻してある。dotnet-performance 側の裏付けは `benchmarks/results/TYP-07-TypeKey.md`(Run 2 / 3a / 3b)。

## 付録 B: 案 B / 案 C のパッチ(`BunnyTail.DependencyInjection/Internal/ServiceRegistry.cs`)

**案 C**(1 行 + クラス): フィールドを

```csharp
private readonly ConcurrentDictionary<Type, ServiceAccessor> activationAccessors = new(TypeHandleComparer.Instance);
```

にし、`AccessorCacheKey` の前に

```csharp
private sealed class TypeHandleComparer : IEqualityComparer<Type>
{
    public static readonly TypeHandleComparer Instance = new();

    public bool Equals(Type? x, Type? y) => ReferenceEquals(x, y);

    public int GetHashCode(Type obj) => (int)(obj.TypeHandle.Value >> 4);
}
```

**案 B**(5 行): フィールド型を `ConcurrentDictionary<RuntimeTypeHandle, ServiceAccessor>`、`Activate` を

```csharp
var handle = type.TypeHandle;
if (!activationAccessors.TryGetValue(handle, out var accessor))
{
    accessor = activationAccessors.GetOrAdd(handle, CreateActivationAccessor(type));
}
```

`CreateFactoryReport` の活性化型ループを

```csharp
var activatedType = Type.GetTypeFromHandle(pair.Key)!;
report.Add(new Diagnostics.ServiceFactoryReportEntry(activatedType, activatedType, null, ServiceLifetime.Transient, status));
```

どちらもビルド 0 警告を確認済み(HX 370)。
