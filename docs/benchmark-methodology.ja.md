# 📐 ベンチマーク実施ガイドライン

**日本語** | [English](benchmark-methodology.md)

パターンの効果検証に使う BenchmarkDotNet の構成と、測定を無意味にする落とし穴の回避策。
[README](../README.md) の「実測例」は本ガイドラインに沿った測定を前提とする。

## 🧰 基本構成

- **MemoryDiagnoser を常時有効化** — 速度とアロケーションは常にセットで判断する
- **DisassemblyDiagnoser(printSource, exportDiff)を有効化** — 生成コードとコードサイズを確認する。速度差が誤差レベルでも、コードサイズで優劣を判断できるケースがある(インライン化への影響はコードサイズに現れる)
- ベンチマークを介さず単発で生成コードを見たい場合は、環境変数 `DOTNET_JitDisasm="メソッド名"` を設定して実行すると JIT アセンブリが標準出力に出る(Release ビルド + `DOTNET_TieredCompilation=0` 併用で最終コードを直接確認)
- **既定は最新ランタイム(net10.0)単独で測定する**。複数ランタイム並走は「世代で効果が変わるか」自体を問う検証(境界チェック除去イディオム、uint キャスト小細工のように新世代で消える最適化)に限定して使う

```csharp
public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddExporter(MarkdownExporter.GitHub);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(
            maxDepth: 3, printSource: true, exportDiff: true)));
        AddColumn(StatisticColumn.Min, StatisticColumn.Max, StatisticColumn.P90);
    }
}

// クラス側: 既定は net10.0 のみ。世代検証の対象クラスに限り net8 等のジョブを追加する
[MediumRunJob(RuntimeMoniker.Net10_0)]
```

## 🅰️ NativeAOT との比較測定

本リポジトリの実測はすべて JIT で取得している。パターンの価値が JIT 固有の機構(投機的脱仮想化・Dynamic PGO・階層昇格)に依存している場合、JIT の数値はそのまま持ち越せず、測る以外に知る方法がない。TYP-07 がその実例で、3 方式の順位が NativeAOT では**反転する**。

素直に試すと 2 点で躓く。しかもどちらも「1 回まるごと実行してから気づく」種類の失敗である。

1. **`DisassemblyDiagnoser` は NativeAOT 非対応。** BDN が検証段階でジョブを拒否し、AOT 行はすべて `NA` になる。しかも終了コードは 0 なので成功したように見える。上記の基本構成は常に診断器を含むため、AOT 比較には診断器を外した別 config が必要(その結果 Code Size 列は使えない)
2. **`vswhere.exe` を `PATH` に通す必要がある**(ILCompiler のリンク段階で使用。実体は `C:\Program Files (x86)\Microsoft Visual Studio\Installer`)。通っていないと `Microsoft.NETCore.Native.targets` 内でリンクが失敗し、やはり AOT 行が全部 `NA` になる

そのため AOT 比較は小さな独立ハーネスで行う。ベンチマーク本体をコピーし、診断器なしの config を与え、両ランタイムをコマンドラインで渡してジョブ設定を揃える。

```csharp
// AOT 比較用の診断器なし config。クラスにジョブ属性は付けず、
// 両ランタイムをコマンドラインで渡して JIT / AOT に同じ MediumRun 設定を適用する
public class AotComparisonConfig : ManualConfig
{
    public AotComparisonConfig()
    {
        AddExporter(MarkdownExporter.GitHub);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddColumn(StatisticColumn.Min, StatisticColumn.Max, StatisticColumn.P90);
    }
}
```

```
dotnet run -c Release -- --filter "*" --runtimes net10.0 nativeaot10.0 --job medium
```

**結果の読み方:** BDN の `Ratio` は**最初のランタイム側**のベースラインメソッドに対して計算されるため、AOT 行もすべて JIT のベースラインに対する比になる。AOT 側を判断するには、AOT 実行自身のベースライン行に対して比を取り直すこと。

**速度ではなく前提を確認する場合:** 正しさのテストでは前提の破綻を隠してしまうことがある。TYP-07 は型ハンドルがポインタ整列であることを根拠に右 3 ビットシフトするが、仮にこれが成立しなくなってもルックアップは正しいまま(挿入時と探索時に同じシフトを掛けるため)で、潰れるのはバケット分布だけである。この種の前提は `PublishAot=true` で publish した native バイナリを直接実行して確認する。なお `PublishAot=true` のプロジェクトに対する `dotnet run` は**依然として JIT 実行**である点に注意 — このプロパティは機能スイッチを切り替えるため `RuntimeFeature.IsDynamicCodeCompiled` が既に `false` を返し、AOT 判定を自前で書くと嘘をつく。`bin/Release/<tfm>/<rid>/publish/<name>.exe` を直接実行すること。

## ⚠️ 測定を無意味にする落とし穴

### 1. 最適化による測定対象の消滅

結果が「空ループの下限値」に張り付いている場合、そのバリアントは JIT に完全に除去されており、実コストの比較になっていない。戻り値を返す・`[MethodImpl(MethodImplOptions.NoInlining)]` を付ける・BenchmarkDotNet の Consumer を使うなどで消滅を防ぐ。逆に、除去されたという事実自体が「その抽象化はゼロコスト」という結論になる場合もあるため、どちらを測っているのか自覚的であること。

### 2. 文字列インターンによる比較の短絡

文字列リテラルをそのままキーに使うと、参照等価により `string.Equals` が中身を比較せず短絡し、比較コードの測定にならない。実運用では外部入力(非インターン文字列)が来るため、プローブ文字列は必ずコピーして生成し、非インターンであることを検証してから測る。

```csharp
var probe = new string(literal.AsSpan());          // 非インターンのコピーを作る
Debug.Assert(string.IsInterned(probe) is null || !ReferenceEquals(string.IsInterned(probe), probe));
```

### 3. バリアント間の等価性未検証

測定前に全バリアントが同じ結果を返すことを `Verify()` として実行する(`BenchmarkRunner.Run` の前に呼ぶ)。速いが間違っている実装を測っても意味がない。手動 ref 走査系は特にバグが混入しやすい(終端 ref の計算ミス、二重走査での ref 進め忘れ等)。実例として、誤った終端判定が「常に真」になっていたループは JIT が判定ごと除去し、境界チェックなしの異常に速い偽の結果を長期間信じさせていた(修正後に再測定したところ最速から中位に転落した)。

**結果が等しいことは、形が等しいことを意味しない。** `Verify()` では「基準だけ呼び出し方が違う」ことは検出できない。LAB-ColumnMatch では基準が JIT にインライン展開される直接呼びで、対抗形はすべて要素ごとに `Func<string,int>` を経由していた — その差は**要素あたり 1.9〜2.2 ns** で、測ろうとしていた差より大きく、結論を反転させた(対抗形が 3.0〜3.6 倍遅いと読めたが、実際は 1.17〜1.19 倍だった)。比率を読む前に、全バリアントが同じ呼び出し形(デリゲート有無、インライン化の結果)を払っているか確認する。直し方は**基準を同じハーネス経由でもう 1 本追加する**ことで、ハーネスを外すことではない。

### 4. 最良ケースだけの測定

宣言順アクセスなど理想形状だけで測ると、実運用で劣化する実装を選んでしまう。アクセス形状(順方向 / 逆順 / 部分アクセス / ミス混在)をパラメータ化し、「平均が速い」ではなく「形状に対して安定」な実装を選ぶ。

### 5. 例外パスの混入・未分離

成功ケースと失敗ケースは `[Params]` で分離して測る。例外スロー 1 回のコストは数 μs 規模で、他の最適化差を完全に覆い隠す(失敗パスが例外の場合、周辺をどれだけ最適化しても無意味になる)。

### 6. マイクロベンチ結果の過信

プリミティブ単体で 30 倍の差があっても、実処理(I/O・描画・支配的な計算)に埋め込むと 1.1 倍程度に希釈された実測例がある。最終判断は実ワークロードに近い形状のベンチマークで行い、マイクロベンチは「どの実装を候補にするか」の選別に使う。

### 7. #if による TFM 依存メソッドの混在(複数ランタイム実行時)

新しいランタイムにしかない API のベンチマークを `#if NET9_0_OR_GREATER` 等で同一クラスに混在させると、ホスト(最新 TFM)が発見したメソッドを下位ランタイムの子ビルドが解決できず、**そのランタイムの全ケースが NA になる**。TFM 依存の比較はクラスごと `#if` で分離し、そのクラスには対応するランタイムのジョブだけを付ける。

### 8. 準備処理の混入

衝突キーの探索・データ生成などの準備は `[GlobalSetup]` で行い、測定対象から外す。`IterationSetup` は測定精度を落とすため、可能な限り GlobalSetup + 状態リセット不要の設計にする。

### 9. 多態な呼び出し site を 1 プロセス 1 回だけ測る

Dynamic PGO はプロセスごとに収束の仕方が変わるため、**多態**(複数の具象型が 1 つの呼び出し site を通る)形状では Tier1 のコードサイズが桁で振れ、**比率の符号まで反転する**。TXT-11 の実測では、同一コードの比が run ごとに 0.91 / 0.97 / 1.05 倍と符号ごと変わり、基準側の Tier1 コードサイズも 1,777〜15,792 B の幅で観測された。多態形状の比較は**複数プロセスで符号の一致を確認する**か、単型に絞って測る。単型(1 つの具象型だけが通る)形状は同じ条件でも安定する。

### 10. 同一命令列でも配置で 2 倍動く(コードサイズ一致 ≠ 同一性能)

判断基準の「信頼区間が重なり、生成コードも一致すれば**差なし**」は正しいが、**逆向きの推論 —「生成コードが一致するのだから性能も同じはず」— は成立しない**。VEC-02 の検証がその反例で、`Vector128.Shuffle` と `Ssse3.Shuffle` は**逆アセンブリが完全一致**(190 B、同じ `vpshufb`)にもかかわらず、x86-64-v3 で 121.53 vs 64.35 ns と **1.76 倍**の再現性ある差が出た。原因はホットループの配置で、片方は 64 バイトの命令フェッチ窓に収まり、もう片方は境界を跨いでいた。

**同じコードを別機で測り直せば決着する。** x86-64-v4 では同じ 2 形が 1〜4% 差に収まり、2 回目は分解すらできない — つまり異常値は 121.53 ns の方だった。**配置由来の差は配置が変われば消えるが、API 由来の差は残る。**

**数パーセントの差にも同じ疑いを向ける。** 7-7 の `Unsafe.BitCast` vs `Unsafe.As` は生成コードが完全一致で、具象 3 形が互いに 1〜2% 以内に並んだが、**同一機の 2 プロセス間で順位が反転**し、プロセス間ドリフト(約 7%)は run 内の差を大きく上回った。**単一プロセスで見た数パーセントの差は、別プロセスで符号が再現するまで対象コードに帰属させてはいけない。**

**切り分け手順:** 予想外の差が出て逆アセンブリが一致した場合は、**同一ソースの複製メソッドを追加して測る**。

| 観測 | 結論 |
|---|---|
| 複製が元と同じ時間になる | 配置由来。API・書き方の差ではない |
| 複製が元と違う時間になる | 配置由来(かつ不安定)。測定条件を疑う |

**宣言順の入れ替えは切り分けにならない。** JIT のコードヒープ配置は宣言順に依存しないため、順序を替えてもアドレスは動かない(VEC-02 で実測確認済み)。ループ開始アドレスは DisassemblyDiagnoser の出力(`printInstructionAddresses`)から読み取れる。

## ⚖️ 判断基準

- **速度・アロケーション・コードサイズの 3 軸**で評価する。1 軸だけの改善は採用理由として弱い
- Ratio のベースラインは「現状の素直な実装」にし、改善幅がそのまま読めるようにする
- 効果が世代で消えた最適化は、パターンとしては「不要になった」と記録する([rejected-patterns.md](rejected-patterns.md) へ)

### 計測が誤差範囲だった場合の扱い

**「ナノ秒単位の差」は誤差ではない。** 本リポジトリの主対象はまさにナノ秒級の差であり、信頼区間が重ならない差は 0.2 ns でも実差として記録する。「誤差」と呼ぶのは**信頼区間(エラーバー)が重なり、統計的に分解できない場合のみ**。その場合も「不採用」と断定せず、**生成コードまで確認して二分する**:

| 生成コードの確認結果 | 記録 |
|---|---|
| 差がある(命令列・コードサイズが異なる) | **➖ 誤差** として記録する。不採用にはしない — 計測分解能以下の差が実在するということであり、コードサイズ・環境・インライン文脈しだいで効く余地を数値つきで残す |
| 一致する(命令列が同一) | **差なし** として不採用側へ。「生成コードが一致した」ことを根拠として記録する |

確認手段は 2 段階:

1. **一次確認**: DisassemblyDiagnoser の Code Size 列。バリアント間でサイズが違えば生成コードは異なる
2. **確定確認**: JitDisasm による命令列の比較。DynamicMethod も名前でマッチできる

```
DOTNET_TieredCompilation=0 DOTNET_JitDisasm="*MethodName*" ./app.exe
```

実例: GEN-01 の「デリゲート Invoke を `Call` で呼ぶ」置換は計測 6.36 vs 6.46 ns の誤差だったが、JitDisasm 比較で **68 命令・229 バイトが完全一致**したため「差なし」と確定した(ターゲットフィールドの読み出しが null チェックを兼ねるため、`callvirt` のチェックが JIT で消える)。

---

## 🗂️ パターン ID の分類基準

族には 2 系統ある。

- **用途ベース** — TXT / COL / SEQ / ASY / DAT / GEN / TYP / SYS(扱う対象・領域で括る)
- **機構ベース** — MEM / STK / BUF / JIT / DSP / BIT / VEC / CON(使う技法で括る)

**振り分けルール:** 該当する**用途族があればそれを優先**し、なければ機構ベースに置く。用途族が**複数にまたがる**場合は代表用途の族に留め、移動先の族を新設しない。

**族の線引き**(判定実績から確定したもの):

| 族 | 何を入れるか | 紛らわしい隣 |
|---|---|---|
| TYP | **型そのものをデータとして扱う**処理(Type キー辞書・型別キャッシュ・キャスト・アクセサ) | JIT との境界 ↓ |
| JIT | **JIT に特化コードを吐かせる書き方**(属性・ジェネリック制約・定数畳み込みされる分岐) | 型引数が主語でも、型を扱うのでなく codegen を誘導するなら JIT |
| DSP | **呼び出しをどう組むか**(sealed / delegate vs interface vs 関数ポインタ / ハンドラ列 / パイプライン) | 脱仮想化という「結果」が同じでも、技法の層が違えば別族 |
| BIT | **ビット表現でキーを畳む・整数演算に落とす**(軽量ハッシュ・マスク・ダイジェスト) | 用途が探索でも、畳み方が主題なら BIT |
| COL | コレクションの探索・変換・内部アクセス | Type がキーなら TYP(TYP-01 / TYP-07 の前例) |
| TXT | 文字列・テキストの生成と判定 | 型ディスパッチが機構でも、対象が文字列生成なら TXT(TXT-11 の前例) |

**判定実績:** 2026-08-19 に 7 件を見直し、**移動は 0 件**だった。

| 対象 | 論点 | 判定 |
|---|---|---|
| BIT-05 順序保存ダイジェスト | 用途は探索(COL)、機構はビット操作 | BIT 維持(BIT-01 と同じ線) |
| TYP-07 Type キーのハッシュ取得元 | 用途はルックアップ(COL) | TYP 維持(TYP-01 と対。Type キーのルックアップは TYP) |
| JIT-02 IEquatable 制約 | 結果が脱仮想化(DSP) | JIT 維持(宣言で codegen を誘導する技法) |
| JIT-03 / JIT-05 | ジェネリック特殊化(TYP) | JIT 維持(定数畳み込みの三点セット) |
| COL-03 GetAlternateLookup | 独自の結果ファイルがない | COL 維持(比較ベンチ 1 本を複数 ID が参照するのは自然) |
| BIT-01 ↔ COL-04 | 1 成果物に 2 ID | 両方維持(ハッシュの作り方 / 実装の選び方で主題が別) |
| TXT-03 Try パターン | 文字列処理でない | TXT 維持(用途族が複数にまたがり移動先が無い) |

---

## 🧪 検証キュー(採否判定の記録)

以下はサンプル作成とベンチマーク実行を行った上で採否を判定する候補。判定の流れ:

1. 候補ごとに検証ベンチマーク(+必要なら最小実装)を作成し、net8 / net9 / net10 で測定する
2. **有効** → パターンとして本文へ収録(実装例・実測付き)
3. **無効** → [docs/rejected-patterns.md](rejected-patterns.md)へ「どの世代まで有効だったか」を付けて記録する
4. **条件付き** → 適用条件を明記して収録する
5. **計測が誤差範囲** → 生成コード(逆アセンブリ)まで確認して二分する。**生成コードに差があれば「➖誤差」として記録**(不採用にしない — 計測分解能以下の差が実在するため、別の軸・環境で効く余地を数値つきで残す)。**生成コードも一致すれば「差なし」として不採用**(コード一致を根拠に記録)。手順は [docs/benchmark-methodology.md](benchmark-methodology.md) の判断基準を参照。なお**ナノ秒単位の差そのものは誤差ではない** — 信頼区間が重ならなければ 0.2 ns でも実差として扱う。「誤差」は信頼区間が重なり統計的に分解できない場合のみ

### ➖ 誤差・差なし判定の記録

計測で分解できなかった差の扱いを、生成コードの確認結果とともに一覧化する(判定の流れ 5. の適用実績):

| 対象 | 計測 | 生成コード確認 | 判定 |
|---|---|---|---|
| GEN-01 デリゲート Invoke の `Call` / `Callvirt` 置換 | 6.36 vs 6.46 ns、信頼区間重複 | JitDisasm 比較で **68 命令・229 バイト完全一致** | ❌ **差なし**(ターゲットフィールド読み出しが null チェックを兼ね、callvirt のチェックが JIT で消える) |
| BUF-03 成長パス(4 KB)の時間 | 1,283 vs 1,427 ns、**信頼区間非重複** | コードサイズは 4,638 vs 997 B で別物 | **実差**(0.90 倍)。割り当て軸(8,056 B → 0 B)でも採用 |
| BUF-04 ラッパー vs 素の Rent/Return の時間 | 1.63 vs 1.65 μs、範囲重複 | — | ➖ **誤差**(時間軸)。ラッパーコストは計測分解能以下。採否は安全性・割り当て軸で判断し採用 |
| COL-06 `ToImmutable` vs `MoveToImmutable`(256 要素)の時間 | 203 vs 171 ns、**信頼区間非重複** | コードサイズ 2,035 vs 891 B で別物 | **実差**(MoveToImmutable が速い)。16 要素でも実差(14.3 vs 11.3 ns)、割り当ては常に半減 |
| STK-08 InlineArray vs stackalloc | 2.92 vs 2.87 ns、信頼区間重複 | コードサイズ 112 vs 134 B で**別物** | ➖ **誤差**(時間軸)。InlineArray の価値は「構造体フィールドに置ける」ことで、コードは僅かに小さい |
| R-18 手書き符号なし範囲チェック | 210.9 vs 211.7 ns、信頼区間重複 | Tier1 で**実質同一**(`sub r8d,100` vs `add r8d,-100` の符号化違いのみ、60 B) | ❌ **差なし**(net10 の JIT は 2 比較形を自動で符号なし 1 比較へ融合する) |
| JIT-01 AggressiveInlining 属性(ループ持ちヘルパー) | 0.943 vs 0.959 μs、信頼区間重複 | 呼び出し側コード**完全一致**(100 B) | ❌ **差なし**(既定ポリシーが既にインライン化。NoInlining のみ +25% の実差 = インライン化自体の価値は実証) |
| STK-07 `new int[0]` vs `Array.Empty` | 0.137 vs 0.140 ns、信頼区間重複 | **同一コード**(どちらも 12 B の共有参照ロード) | ❌ **差なし**(net10 では両者とも割り当てゼロかつ同一コード。`[]` はスタイルとしての既定) |
| DSP-01 インターフェース参照越しの sealed 有無 | 220.7 vs 221.9 ns、信頼区間重複 | コードサイズ 84 B で一致(一次確認) | ➖ **誤差**。具象 sealed 型保持は時間 約 2% + コードサイズ 27 vs 84 B(実利はコードサイズ/AOT 側) |
| COL-02 Frozen の検索(string キー 16 / 256 件) | 1.00 / 0.98 倍、信頼区間重複 | — | ➖ **誤差**。検索利得がないため 8〜11 倍の構築コストが償却されず不採用条件に該当 |
| R-02 範囲保証済みランダムアクセスの ref 化 | 245.2 vs 246.3 ns、信頼区間重複 | コードサイズ 55 vs 72 B | ➖ **誤差**。境界チェック除去の利得は実質ゼロ(逐次走査では自動ベクトル化を阻害して 1.05 倍の実害) |
| R-02 サンプリングアクセス(Span 3 位置)の手動 ref | 時間は分解能以下 | **別物**(索引形は境界チェック 1 本残存 = RNGCHKFAIL 経路、128 vs 115 B・56 vs 49 命令) | ➖ **誤差**。構成的に範囲保証されるホットパス(SampledNameTable.CalculateHash)では手動形を維持 |
| R-01 typeof の static readonly キャッシュ | 完全に同値 | Tier1 で**同一の即値ロードに一致**(11 B。昇格前はキャッシュ側に初期化チェックが残り 48 B) | ❌ **差なし**(コールドパスではキャッシュ側が不利ですらある) |
| R-04 ループ構文 for / while | 完全に同値 | **命令列一致**(28 B) | ❌ **差なし**(「正規化」が成り立つのはこの 2 形式) |
| R-04 do-while / 降順 for | 完全に同値 | **別物**(do はループ内境界チェック残存 63 B、降順はクローン 85 B) | ➖ **誤差**。既定は for / while |
| R-04 foreach / for(配列・Span) | 配列 2 形式は 212.4 vs 212.6 ns、Span 4 形式は 0.5 ns 以内 | **命令列一致**(配列 32 B / Span 54 B) | ❌ **差なし**。可読性で選ぶ |
| R-04 配列をフィールド経由で回す for | x86-64-v4 で 1.13 倍、**信頼区間非重複**(x86-64-v3 では 2.20 倍) | **別物**(境界チェック残存・参照再ロード 67 B) | ✅ **実差**。ただし倍率はコア依存。参照はローカルへ退避する |
| STK-03 単相 `IEnumerable<int>` の foreach(.NET 10) | 配列直接の 1.09 倍(234 vs 214 ns) | 列挙子がスタック化(0 B、コード 206 B) | ✅ **前提の変化**。多相は 8.9 倍・36 B に戻る |
| DSP-04 ループ内キャプチャ(.NET 10) | 8.18 倍(257.0 vs 31.4 ns)、static + TState は 0.74 倍 | **消去せず**(88 B/反復のまま、反復ごとに `CORINFO_HELP_NEWSFAST` × 2) | ❌ **指針不変**。static + TState のみ 0 B。公式記事の消去は 2 機とも再現せず |
| R-09 Int32 デコード: Cast vs ポインタ | Cast 0.30 / ポインタ 0.35(215.2 vs 249.0 ns、信頼区間非重複。x86-64-v3 では 0.26 / 0.52) | Cast 54 B / ポインタ 97 B(内側ループ 5 vs 6 命令、ポインタは `movsxd` が依存チェーンに乗る) | ✅ **Cast が速い**(1.16 倍、x86-64-v3 では 2 倍)。外部報告のポインタ優位(0.19)は 2 機とも再現せず |
| R-15 別シーケンスの Length を添字に | Span 1.09 倍(337.7 vs 309.1 ns、x86-64-v3 では 2.05 倍) | string/配列は消える(22 B)、**Span は残る**(48 B、RNGCHKFAIL)— 2 機で同一 | ✅ **非対称を確認**。ホットパスは string/配列で受ける |
| R-22 Ankerl 方式 vs Dictionary | 探索 1.48 倍遅い(5.82 vs 3.93 μs、同一ハッシュでも 1.48 倍)、ミス 1.58 倍、構築 1.21 倍、いずれも信頼区間非重複(x86-64-v3 でも 1.26〜1.66 倍) | — | ❌ **不採用**。基準値の外れ値が 3.8 倍の正体。2 機で符号一致 |
| R-23 InlineList(収まる / 溢れる) | 収まれば 0.49 倍(4.3 vs 8.8 ns)/ 溢れると容量指定 List に 1.76 倍負け(44.1 vs 25.0 ns)、いずれも信頼区間非重複(x86-64-v3: 0.35 倍 / 1.62 倍) | 0 B / 240 B(容量指定 List は 184 B) | ❌ **不採用**。勝てる条件(上限既知)では STK-08 で足りる。スピル時の負けは 2 機で再現 |
| R-04 `List<T>` を添字 for で回す | x86-64-v4 で 1.29 倍、**信頼区間非重複**(x86-64-v3 では誤差内) | **別物**(索引形は 1 ステップに境界チェックが **2 本**、Count と配列、72 vs 71 B) | ✅ **実差**。倍率はコア依存。foreach か `CollectionsMarshal.AsSpan`(0.85 倍)を選ぶ |
| R-10 インスタンス readonly フィールド | 0.006〜0.016 ns で測定不能 | 読み出しは**オフセット以外同一**(4 B) | ❌ **差なし**(インスタンス readonly は JIT 最適化に寄与しない) |
| R-14 コピーの CopyBlockUnaligned 置換 | 可変長 512 B 以上で 0.92〜1.01 倍、定数長 8 B 0.89 倍 / 16 B 0.94 倍 — いずれも信頼区間重複。定数 64 B では**1.07 倍(遅い)**、信頼区間非重複 | 呼び出し形は異なる(52〜64 B vs 96〜102 B)が、両者とも**同じ Memmove に到達** | ➖ **誤差**(信頼区間が重なる範囲)。64 B で符号が逆転する。残る利点はコードサイズのみで、安全性の放棄に見合わない |
| 7-1 `scoped` の有無(span / ref 引数) | 60.50 vs 60.83 ns / 1.511 vs 1.516 ns、信頼区間重複(x86-64-v3: 69.22 vs 70.00 / 2.276 vs 2.296) | caller / callee とも**命令列完全一致。しかも両機でバイト単位まで同一の値**(89 / 35 B、50 / 38 B) | ❌ **差なし**(純粋なコンパイル時契約で ISA 非依存。安全性ツールとして STK-01 に収録) |
| 7-1 `[UnscopedRef]` ref 返しアクセサ | 0.3881 vs 0.3943 ns、信頼区間重複(x86-64-v3: 0.5013 vs 0.5349) | **別物**だが命令数は一致。コードサイズは**機械で符号が反転**(v3 で 85 → 88 B、v4 で 85 → 81 B)し、差の正体はアラインメント nop(実コードは 71 対 72 B) | ❌ **不採用**(どちらの機でもどの軸にも改善なし → R-20) |
| 7-7 `Unsafe.BitCast` vs `Unsafe.As` | 互いに 1〜2% 以内で、**同一機の 2 プロセス間で順位が反転**(`Unsafe.As` が run 1 で最遅 222.9 ns、run 2 で最速 230.5 ns)。プロセス間ドリフト(約 7%)が run 内の差を上回る | **命令列完全一致**(具象 57 B ×3、ジェネリック 21 B ×2、両機とも) | ❌ **差なし** → ただし**安全性軸で採用**(同一コードは「乗り換えコストゼロ」の証明) |
| 7-3 `GetValueRefOrNullRef` の読み取り経路・**8 バイト値** | 0.99〜1.04 倍、両機とも信頼区間重複(6 ns だけ重なった 1.04 倍は再測定で消えた) | 命令数一致(200 vs 200)、違いは基本ブロックの配置のみ | ❌ **差なし**(1 フィールド読みでは分解できない) |
| 7-3 同・**32 バイト値 / 2 フィールド読み** | x86-64-v4 で 0.93 倍・0.94 倍、**2 回とも信頼区間非重複**。x86-64-v3 では 1.00 倍 | 202 vs 201 命令。ref 形は各フィールド読みを `add rsi,[r13]` に畳み、`TryGetValue` 形は `mov` + `add` を出す | ✅ **実差** — 節約はフィールド読み 1 つにつき 1 命令。複数フィールドを読み、かつコアが十分広いときだけ顕在化する → COL-07 |
| 7-9 `Vector128.Shuffle` vs `Ssse3.Shuffle` | x86-64-v3 で 121.53 vs 64.35 ns、**信頼区間非重複**。x86-64-v4 では同じ比較が 62.51 vs 61.06(run 1)、64.69 vs 62.21 ns(run 2、**信頼区間重複**) | **命令列完全一致**(59 命令 / 190 B、両機とも) | ⚠️ **配置由来と確定**。API 差ではない。別機で 1.76 倍が 1〜4% に収縮したので、異常値は 121.53 ns の方(落とし穴 10 を参照) |

| 批次 | 候補 | 概要 / 検証の問い | 関連 | 状態 |
|:---:|---|---|---|:---:|
| ① | RuntimeHelpers.IsReferenceOrContainsReferences\<T\> 分岐 | 参照を含まない T でクリア・コピー処理をスキップ。JIT が定数化して分岐ごと消えるか | JIT-03 | ✅ 収録([JIT-05](../README.ja.md#️-jit-05-isreferenceorcontainsreferences-による処理スキップ)) |
| ① | Unsafe.CopyBlockUnaligned | Span.CopyTo / Array.Copy に対して優位になる条件の特定(定数長で mov 列に展開される場合のみか) | MEM-03 / SEQ-02 | ❌ 不採用一覧へ |
| ① | 末尾要素の事前アクセスによる境界チェック除去 | `_ = array[length - 1]` の事前タッチ・逆順アンロール。.NET 8 有効 / .NET 10 で差消滅の再確認(不採用想定) | MEM-01 | ❌ 不採用一覧へ |
| ① | GC.AllocateUninitializedArray\<T\> | 大配列のゼロ初期化スキップ。効果が出るサイズ閾値の特定 | BUF-01 / BUF-05 | ✅ 条件付き収録([BUF-06](../README.ja.md#-buf-06-gcallocateuninitializedarray-によるゼロ初期化スキップ)) |
| ① | 定数サイズ stackalloc | 定数確保+スライス vs 可変サイズ(localloc 命令)のコスト差 | BUF-03 / BUF-05 | ✅ 収録([STK-06](../README.ja.md#-stk-06-定数サイズ-stackalloc)) |
| ② | CollectionsMarshal.SetCount(.NET 8+) | Add ループ(容量チェック×N)vs SetCount + Span 直接書き込み。未初期化領域が見える危険の注意付き | COL-01 | ✅ 収録(COL-01 拡張、0.22〜0.26 倍) |
| ② | IEnumerable\<T\> 引数の具象型分岐 | `is T[]` / `is List<T>` / TryGetNonEnumeratedCount で Span パスへ逃がす LINQ 内部の定石 | COL-04 / STK-02 | ✅ 条件付き収録([COL-05](../README.ja.md#️-col-05-ienumerable-引数の具象型ディスパッチ)。List 1.8 倍、配列は GDV により利得なし) |
| ② | COL-01 の実装例・自環境再測定 | AsSpan / GetValueRefOrAddDefault(収録済みパターンの実装例化) | COL-01 | ✅ 検証済(AsSpan 0.52 / ref 化 0.66) |
| ③ | byte 列の int 化定数比較 | 短い ASCII トークン(HTTP メソッド等)を uint/ulong 定数 1 比較で判定 vs `SequenceEqual("..."u8)` | BIT-01 / TXT-01 | ✅ 収録([TXT-04](../README.ja.md#-txt-04-バイト列トークンの直接判定)。string 化回避が本質、uint と SequenceEqual は同速) |
| ③ | Utf8.TryWrite(.NET 8+) | UTF-8 補間ハンドラによる Span\<byte\> 直接整形。TXT-01 テーブル方式との比較 | TXT-01 / BUF-02 | ✅ 収録([TXT-05](../README.ja.md#-txt-05-utf8trywrite-による-utf-8-直接整形)、0.54 倍・0B) |
| ③ | ASCII 特化処理 | Ascii クラス(.NET 8)/ char.IsAsciiXxx / `& 0x5F` 大文字化による ASCII 前提の高速パス | BIT-01 / TXT-01 | ✅ 収録([TXT-06](../README.ja.md#-txt-06-ascii-特化比較)、0.62 倍。手書き正規化は記号衝突の注意付き) |
| ③ | BUF-02 の実装例(I/O 直結) | MemoryStream 蓄積 vs ArrayBufferWriter vs 自前 PooledBufferWriter(収録済みパターンの実証) | BUF-02 | ✅ 実装済(PooledBufferWriter。アロケーション 2,976B→32B) |
| ④ | async ステートマシンの省略 | 単純フォワードの Task 直接返し vs async/await。例外発生位置・using スコープが変わる注意付き | TXT-03 / 拡充候補 ValueTask | ✅ 収録([ASY-01](../README.ja.md#-asy-01-async-ステートマシンの省略)、0.16 倍・73B→0B) |
| ④ | Environment.TickCount64 / Stopwatch.GetTimestamp | DateTime.UtcNow(十数 ns)を回避する時刻・経過時間取得。キャッシュ TTL・タイムアウト用途 | — | ✅ 収録([SYS-01](../README.ja.md#️-sys-01-低コストの時刻経過時間取得)、TickCount64 は 22 倍) |
| ④ | pinned バッファ(GC.AllocateArray(pinned: true)) | POH 常駐 I/O バッファによるピン止めコスト回避 | BUF-01 / BUF-02 | ❌ 性能目的は不採用一覧へ(fixed は実測無料。POH は長寿命断片化対策専用) |
| ④ | BitOperations 活用 | TrailingZeroCount / PopCount / Log2 によるスキャン・計算のループ除去 | BIT-02 | ✅ 収録([BIT-03](../README.ja.md#-bit-03-bitoperations-によるビット走査計数)、走査 7.6 倍・PopCount 67 倍) |
| ⑤ | SIMD 実装例(Vector128/256) | 合計・検索・変換の明示的 SIMD 化。スカラー・`Vector<T>`・組み込み関数の比較 | JIT-02 / BIT | ✅ 収録([VEC-01](../README.ja.md#-vec-01-明示的-simdvectort--vector256)、Vector256 8.9 倍。BCL 済み API 優先の指針付き) |
| ⑤ | ref フィールドによる ref struct 設計(C# 11) | カーソルを Span + index でなく ref T で保持する設計のコスト比較 | STK-01 | ❌ 反復用途は不採用一覧へ(for 比 1.21 倍で利得なし) |
| ⑤ | P/Invoke 高速化 | \[LibraryImport\] + Span 渡し + \[SuppressGCTransition\](短時間ネイティブ呼び出しの GC 遷移省略)の効果と制約 | BUF-05 | ❌ 不採用リストへ移動(R-19。LibraryImport は標準の宣言方法であって最適化ではない。SuppressGCTransition は計測で利得なし) |
| ⑤ | System.Threading.Channels | 生産者消費者キュー。Bounded/Unbounded・SingleReader/SingleWriter オプションの効果 | DSP-03 | ✅ 収録([ASY-02](../README.ja.md#-asy-02-systemthreadingchannels-による生産者消費者)、~45ns/要素・Bounded は 2 倍) |
| ⑤ | System.IO.Pipelines | PipeReader/PipeWriter による I/O パイプライン。Stream 直接処理との比較 | BUF-02 | ✅ 条件付き収録([ASY-03](../README.ja.md#-asy-03-systemiopipelines)、小データは 1.63 倍・アロケーション 1/80。64KB デッドロック注意) |
| ⑤ | IAsyncEnumerable のコスト | await foreach の要素あたりオーバーヘッド(vs IEnumerable / Channel)、\[EnumeratorCancellation\] の作法 | SEQ-03 | ✅ 収録([ASY-04](../README.ja.md#-asy-04-iasyncenumerable-のコスト認知と使い分け)、要素あたり 11.6 倍のコスト認知) |
| ⑥ | net11 世代ウォッチ | net11 GA 後に再測定: ① enum の Equals 経由ボックス化が JIT 特殊化で消える(STK-05 の暗黙ボックス化リストへ世代注記)② LINQ Min/Max のベクトル化(VEC-01 の「BCL 済み API 優先」指針の裏付け強化) → 追加分は下記「net11 GA 時の再検証項目」(A1〜G2) | STK-05 / VEC-01 ほか | ⏳ net11 GA 待ち |
| ⑦ | `scoped` / `[UnscopedRef]`(C# 11) | codegen に出るか。ref 返しアクセサは get/set ペアに勝つか | STK-01 | ❌ 差なし / 不採用(R-20)。`scoped` は STK-01 の注記へ |
| ⑦ | ref フィールドの構造読み | R-12 が名指しした「フィールド粒度の読み」で索引形に勝つか | STK-01 / R-12 | ✅ 収録([STK-10](../README.ja.md)、x86-64-v4 で 0.81 倍 / v3 で 0.75 倍、生成コードは同一) |
| ⑦ | `GetValueRefOrNullRef` + `IsNullRef` | 存在確認つき更新を探索 1 回に畳めるか | COL-01 | ✅ 収録([COL-07](../README.ja.md)、更新 0.44〜0.59 倍。読み取りは 1 フィールドなら差なし、32 バイト値を 2 フィールド読む形は x86-64-v4 で 0.93 倍) |
| ⑦ | struct レイアウト(サイズ・フィールド順) | 32 → 24 バイトが走査・引数渡しに出るか | MEM-02 / MEM-04 | ✅ 条件付き収録([MEM-05](../README.ja.md))。条件はバイト数ではなくキャッシュ容量の境界: x86-64-v3(512 KB vs 384 KB が L2 512 KB を跨ぐ)で散在 0.67〜0.71 倍、x86-64-v4(L2 1 MB)では差なし。値渡しは両機で逆転 |
| ⑦ | false sharing / キャッシュラインパディング | 罰則の大きさ。64 バイトで足りるか | CON-01 | ✅ 収録([CON-03](../README.ja.md)、4〜29.7 倍)。パディングの有無は無条件、サイズは条件付き: x86-64-v3 は 128 バイト必須、v4 は 64 バイトで十分 — プリフェッチ粒度で選ぶ |
| ⑦ | `Memory<T>` の `.Span` コストと配列橋渡し | ホイストの効果、裏の実体依存、`TryGetArray` | BUF-04 | ✅ 収録([BUF-08](../README.ja.md)、要素ごとで 2.96〜3.11 倍)。コストは `.Span` 解決 1 回ごとなので、16 バイトチャンクごとなら 1.05 倍 |
| ⑦ | `Unsafe.BitCast`(.NET 8+) | `Unsafe.As<TFrom,TTo>` に対してコストが増えないか | JIT-03 / SEQ-02 | ✅ 収録(生成コード一致 → 安全側の既定として TYP-05 / JIT-03 / SEQ-02 に追記) |
| ⑦ | `MemoryMarshal.Cast` の実コストと落とし穴 | 手動再解釈との優劣、切り捨て・アラインメント | BIT-04 / R-09 | ✅ 注記として収録(早見表・BIT-04・R-09 に条件追記) |
| ⑦ | 固定幅組み込み関数(Shuffle) | `Vector<T>` で書けないレーン置換の効果 | VEC-01 | ✅ 収録([VEC-02](../README.ja.md)、x86-64-v4 で 0.29 倍)。ISA 直呼びの優位は配置由来で否定 — 別機で 1.76 倍が 1〜4% に収縮することで再確認 |
| ⑦ | `MemoryManager<T>` / `NativeMemory` | アンマネージド領域の `Memory<T>` 化コスト | BUF-04 / R-13 | ✅ 収録(BUF-08 に同居) |
| ⑦ | `AreSame` / `ByteOffset` / `Overlaps` | ref からの index 復元、別名検査のコスト | R-02 | ❌ index 復元は不採用(R-21)。別名検査は早見表へ |
| ⑦ | `Unsafe.Unbox<T>` | 既存ボックスを再確保せず更新できるか | STK-05 | ✅ 収録(STK-05 拡張、0.17〜0.18 倍・割り当てゼロ。消えるのが割り当てなので比率は機械非依存) |
| ⑦ | `MemoryMarshal.TryGetArray` | `byte[]` 前提 API への無コピー橋渡し | BUF-04 | ✅ 収録(BUF-08 に同居、割り当て 4,120 → 0 B) |
| ⑧ | 序数 switch を使うためのプローブ正規化(列名照合) | 先に大文字化する形は `Equals(OrdinalIgnoreCase)` / サンプリングハッシュ switch に勝てるか | TXT-10 / GEN-02 | ⚠️ **判定は二分。** 変換は**不採用** — 測定 12 条件すべてで負け(8 列 2.0〜3.0 倍、24 列 1.35〜2.91 倍)、列あたり 2.7〜3.2 ns を足す。照合そのものの境界は列数で、8 列は連鎖の勝ち(switch が 1.17〜1.19 倍)、24 列は**変換なし**の序数 switch の勝ち(0.91 倍、コード 2,212 対 3,261 B)。等価比較はハーネスを揃えた基準を追加して初めて成立した — 落とし穴 3 を参照 → [LAB-ColumnMatch](../benchmarks/results/LAB-ColumnMatch.md) |
| ⑧ | Type キー辞書のキー型(`Type` vs `RuntimeTypeHandle`) | TYP-07 は自作表内のハッシュ取得元を比較した。BCL `Dictionary` に留まる場合、キーの型を `RuntimeTypeHandle` にするだけで速くなるか。自作表との差は残るか | TYP-07 / TYP-01 / R-22 | ⏳ 仮測定(B550H)で `RuntimeTypeHandle` キーが hit 0.70 倍・miss 0.77 倍(信頼区間非重複、コード 1,037 → 797 B)。自作表 + `TypeHandle.Value` は 0.29 倍で 2.4 倍先。**2 つの判断は独立** — HX 370 と NativeAOT で確定 |

---

### 🔭 net11 GA 時の再検証項目(ライブラリの書き方が変わるものに限定)

出典: [Performance Improvements in .NET 11](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-11/)(.NET Blog、2026-09)。**「機能が速くなった」だけの項目は載せない。** 載せるのは「手書きの回避策が不要になる」「カタログの指針が変わりうる」ものだけで、LINQ の改善は対象外。各行は「net11 で何が変わったか → カタログのどこが影響を受けるか → 何で測るか → どの結果ならカタログを書き換えるか」。

**共通の判定方針:** 時間ではなく**生成コード(RNGCHKFAIL の有無・コードサイズ)と確保バイト数**で判定する(R-04 で倍率がコア依存だった経緯のとおり)。既存ベンチマークは `RuntimeMoniker.Net11_0` の Job を足すだけで再実行できる。

#### A. 境界チェック除去の拡張 — 手動 ref / unsafe の「例外」が縮む

| # | .NET 11 の変更(PR) | 影響する記述 | 測るもの | 書き換え条件 |
|---|---|---|---|---|
| A1 | 定数添字の連続アクセスを 1 本のガードに畳む(#127439 / #124705)。`values[0]..values[15]` は最大添字の 1 回チェックに。Sum16 0.62 倍 | R-15(末尾の事前タッチ)、外部記事「消えるパターン集」#5 | `LAB-BoundsCheckHint` を net11 で再実行 | 手書きの事前タッチが**逆に遅い / コードが増える**なら R-15 を「不要」から「有害」へ格上げ |
| A2 | 先読みガード `(uint)(i + n) < (uint)span.Length` が `span[i]..span[i+n]` を証明(#124242 / #125235) | R-02 の例外(2)「構成的に範囲保証されたサンプリングアクセス」= `SampledNameTable.CalculateHash` の手動 ref | `SampledNameTable` の索引形 vs 手動 ref を net11 で再測 | 索引形で RNGCHKFAIL が消えるなら**手動 ref を索引形へ戻し、R-02 の例外(2)を削除** |
| A3 | `Slice(Length - n)` 後の固定幅読み(#127488、73 → 28 B)と、Slice 前進ループの追跡(#122040 / #127117) | SEQ-02(`BinaryPrimitives` の末尾読み)、VEC-01 のチャンクループ(`data = data.Slice(16)`) | 両形のコードサイズと RNGCHKFAIL | チェックが消えるなら「Slice 形でよい、index 算術に開く必要なし」を SEQ-02 / VEC-01 に追記 |
| A4 | `BitOperations.LeadingZeroCount / TrailingZeroCount / PopCount` の結果範囲を認識(#128620、0.85 倍)、ビット OR の範囲合成(#122263) | BIT-03、TXT-01(テーブル索引)、R-09 | `table[BitOperations.Log2(v)]` と Base64 型 `(a << 4) \| (b >> 4)` 索引のコードサイズ | 消えるなら「ビット演算由来の添字は unsafe 不要」を BIT-03 / TXT-01 に追記 |
| A5 | `(uint)i < (uint)span.Length` ガードが `i >= 0` も記録(#125056) | R-18(uint キャストの手書き) | `span[i - 1]` を含む形で uint ガードの有無を比較 | uint ガードが**時間で**効く形が見つかれば、R-18 の「意味がない」に条件を付ける |
| A6 | ガード後の `checked(...)` のオーバーフロー分岐を除去(#124147 / #124184、64 → 44 B・52 → 24 B) | (未収載)ホットパスで `checked` を避ける慣習 | ガード付き `checked(length * 10)` / `checked((byte)value)` のコードサイズ | 消えるなら「範囲ガード後は `checked` を書いてよい」を新規注記(安全性側の改善) |
| A7 | `!=` 終端ループのクローン(#129268 / #129303) | R-04(昇順 `<` を推奨) | `for (i = 0; i != n; i++)` を `LoopFormBenchmark` に追加 | `<` と同一命令列なら R-04 の推奨を「`<` または `!=`」へ緩和 |

#### B. エスケープ解析の拡張 — 確保回避の手書きが不要になる

| # | .NET 11 の変更(PR) | 影響する記述 | 測るもの | 書き換え条件 |
|---|---|---|---|---|
| B1 | 委譲する `GetEnumerator()` 連鎖を条件付きエスケープ解析が認識(#122946)。`ReadOnlyInstance` 13.874 → 2.674 ns、32 → 0 B | **STK-03、R-04 のラッパー節(⏳❗ 本検証待ち)** | `ReadOnlyCollectionLoopBenchmark` / `EnumerableEscapeBenchmark` を net11 で再実行 | ラッパーの foreach が 0 B になれば R-04 の「ラッパーは添字」を net10 限定に格下げし、STK-03 の適用範囲をさらに狭める。net10 で「再現せず」だった原因が世代差だったと確定する |
| B2 | `Nullable<T>` のボックス化を JIT が展開しエスケープ解析の対象に(#122167、0.21 倍・24 → 0 B) | STK-05(暗黙ボックス化リスト)、上表 ⑥ の ① | STK-05 の暗黙ボックス化ケースに `T?` 経由を追加して再測 | 消えるケースをリストから外し「net11 以降は不要」と世代注記 |
| B3 | `EqualityComparer<T>.Default.Equals` の受け手を間接参照でなく直接ロード(#121918、0.46 倍・24 → 0 B) | JIT-02(`IEquatable<T>` 制約) | JIT-02 の比較を「制約あり」vs「`EqualityComparer<T>.Default` 直呼び」で再測 | 差が消えれば JIT-02 を「制約は AOT / 旧世代向け」に条件付け |

#### C. devirt の拡張

| # | .NET 11 の変更(PR) | 影響する記述 | 測るもの | 書き換え条件 |
|---|---|---|---|---|
| C1 | ジェネリック仮想メソッド(GVM)の devirt + インライン化(#122023 / #128702、0.25 倍・24 → 0 B) | (未収載)DSP-01 / JIT-02 の周辺 | インターフェース上の `T Get<T>()` 形を sealed 実装で呼ぶベンチを新設 | net11 で直接呼び出しになるなら「GVM をホットパスで避ける」という慣習は不要、と DSP-01 に注記 |

#### D. runtime-async(オプトイン)

| # | .NET 11 の変更 | 影響する記述 | 測るもの | 書き換え条件 |
|---|---|---|---|---|
| D1 | `<Features>runtime-async=on</Features>` で async の下位変換を C# コンパイラでなく JIT が担当。2 層チェーン 21.2 → 6.2 ns・144 → 0 B、10 層の例外伝播 0.30 倍、バイナリ 0.52 倍 | ASY-01(async 消去)、ASY-05(ValueTask)、ASY-04 | 既存の ASY-01 / ASY-05 ベンチをフラグ on / off の 2 Job で再測 | on で `return await` の確保が消えるなら ASY-01 を「フラグ off または net10 以前向け」に条件付け。`ValueTask` を選ぶ動機(同期完了時の確保回避)も再判定 |

#### E. コアライブラリ API の追加 — 自前実装を BCL へ置き換える

| # | .NET 11 の変更(PR) | 影響する記述 | 測るもの | 書き換え条件 |
|---|---|---|---|---|
| E1 | `MemoryExtensions` に `ContainsAnyWhiteSpace` / `IndexOfAnyWhiteSpace` / `IndexOfAnyExceptWhiteSpace` / `LastIndexOfAny(Except)WhiteSpace` を追加(#111439)。内部は共有の `SearchValues<char>` | TXT-08(自前の空白 `SearchValues`)、トリム・パーサ実装 | 自前 `SearchValues.Create(" \t\r\n…")` と新 API の時間・コードサイズ。**トリムのように「ほぼ何も無い」入力**では記事自身が「スカラーの方が速い場合がある」と注記しているので、その形も測る | 同等以上なら「空白探索は自前 SearchValues を持たず BCL API」を TXT-08 に追記。短い入力でスカラーが勝つ条件が出れば併記 |
| E2 | `Span<T>.Sort<T, TComparer>` が struct 比較子を**ボックス化せず**ジェネリック特殊化(#116109)。0.30 倍・88 → 0 B | **R-06 / JIT-02**(「比較子は struct + ジェネリック制約で渡す」)。**net10 まではこの指針が `Span.Sort` では効いていなかった**(ボックス化 + インターフェース呼び出し) | net10 と net11 で struct 比較子渡しの `Span.Sort` の確保バイト数 | net10 で 88 B が出れば R-06 に「`Span.Sort` での struct 比較子は net11 から有効」と世代注記。JIT-02 の適用範囲の記述も見直す |
| E3 | `DeflateEncoder/Decoder`、`ZLibEncoder/Decoder`、`GZipEncoder/Decoder` を公開(#123145、既存の `BrotliEncoder` と同形)。呼び出し側が入出力バッファを所有・プールできる | BUF-05 / ASY-07、Rester の `CompressedContent`(GZipStream / DeflateStream) | `GZipStream` 経由と `GZipEncoder` + プール済みバッファでの確保・時間 | 確保が消えるなら「Stream ラッパーを介さない Span 圧縮」を BUF 系に新設し、Rester を適用候補に |
| E4 | portable なレーン操作 API を追加(#129627): 系列生成(`[1, 2, 4, 8]`)、半分連結、interleave / de-interleave、reverse。JIT が命令選択 | **VEC-02**(`Ssse3` vs `Vector128.Shuffle`)、VEC-01 | VEC-02 のシャッフルを新 API で書いた版のコード生成と時間 | 同一命令列なら VEC-02 に「プラットフォーム固有 API を portable へ置換可」を追記 |
| E5 | SIMD 型へ `Unsafe.BitCast` されるユーザ定義 struct を struct promotion の対象外にし、ベクタ表現を維持(#129563) | LAB-BitCast、VEC-01(`Vector2Double` 型の値型) | 2 つの `double` を持つ struct ↔ `Vector128<double>` の往復で spill が消えるか | 消えるなら「ドメイン型は BitCast で SIMD 演算してよい」を VEC-01 に追記 |

#### F. 既存判定の数値が動くもの

| # | .NET 11 の変更(PR) | 影響する記述 | 測るもの | 書き換え条件 |
|---|---|---|---|---|
| F1 | `FrozenDictionary` 構築時の一時 `Dictionary` を元の件数で事前サイズ確保(#128300) | **COL-02 / R-08**(不採用の主因は「構築が `Dictionary` の 5〜20 倍」) | COL-02 の構築ベンチを net11 で再測 | 構築比が縮めば COL-02 の採用条件を緩和。検索側は変わらないので「1024 件で検索 1.19 倍遅い」は残る |
| F2 | ジェネリック `T : Enum` 文脈の `Enum.Equals` を基底整数の直接比較へ畳む(#122779)。従来はボックス 2 回 + 仮想呼び出し | 上表 ⑥ の ①、STK-05(暗黙ボックス化リスト) | STK-05 の enum 比較ケースを net11 で再測 | 0 B になれば STK-05 のリストから外し「net11 以降は不要」と世代注記。`EqualityComparer<T>.Default` や `Unsafe.As` による回避策も不要になる |
| F3 | 参照型配列への格納で、JIT が配列の正確な型を知る場合は共変性チェックを除去(#126547) | MEM-02、DSP-01(sealed) | `object[]` への格納 vs sealed 要素型の配列への格納 vs ローカル生成配列への格納 | チェックが消える条件が確認できれば DSP-01 の効果に「配列格納の共変性チェック回避」を追加 |

#### G. 注記のみ(この環境では計測不能、または net11 を待たず適用可)

| # | 内容 | 扱い |
|---|---|---|
| G1 | ロック / `Interlocked` / 一度きり初期化で既に保護されているフィールドの `volatile` は冗長(#125274 で BCL 全体から除去)。x64 では命令に出ないが **Arm ではフェンスになる** | CON 系の指針として注記する。x64 環境では計測できないため、判定は記事の記述に依拠すると明記 |
| G2 | 固定書式の解析・整形で**先頭に `span = span[..N]` を置いて厳密な長さを確定**させると、後続の定数添字の境界チェックが全て消える(#119254 が `Decimal` / `Guid` / `IPAddress` に適用) | **net11 を待たず net10 で検証可能。** TXT-09(固定長整形の応用イディオム)の候補として先に測る |

**除外したもの(性能向上のみで書き方は変わらない):** デリゲートの 8 B 縮小(#99200。DSP-04 の表の数値は動くが指針は不変)、同一ブロック内アサーション追跡(#121527)、list pattern の境界チェック(#121273)、intrinsic 型のインライン予算緩和(#127433)、spill 越しの型情報保持(#128485)、小さな struct のレジスタ渡し改善(#112740。MEM-04 の数値は動く)、R2R / NativeAOT の GVM devirt、Arm64 のコード生成全般(ペアロード・shrn・ubfx 等)、SVE(実験的)、Tier0 の Nullable ボックス化、`string.Concat` / `string.Split` / `Ascii.Equals` の高速化、`Dictionary.Remove` の値型キー最適化(#125884。R-22 の「Dictionary は既に速い」を補強するのみ)、`HashSet.UnionWith` / `Array.FindAll` / `ImmutableArray` の内部改善、`Random` の `[NoInlining]`(#131714。JIT がループ内 if 変換に対応するまでの暫定策と記事自身が明記)、BigInteger / TensorPrimitives / TimeZoneInfo / Regex / Networking / Diagnostics、LINQ 全般。

**確認範囲:** 記事全文(Benchmarking Setup 〜 What's Next、379 KB)を通読した上での選別。A〜D は JIT 節、E〜G は Vectorization / Threading / Numerics / Strings and Spans / Collections 節に基づく。
