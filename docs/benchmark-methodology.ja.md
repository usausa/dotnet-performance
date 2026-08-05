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
| BUF-03 成長パス(4 KB)の時間 | 1,283 vs 1,427 ns、**信頼区間非重複** | コードサイズは 4,638 vs 997 B で別物 | **現在は実差**(0.90 倍)。旧ベースラインでは誤差だった。採否はいずれにせよ割り当て軸(8,056 B → 0 B)で採用 |
| BUF-04 ラッパー vs 素の Rent/Return の時間 | 1.63 vs 1.65 μs、範囲重複 | — | ➖ **誤差**(時間軸)。ラッパーコストは計測分解能以下。採否は安全性・割り当て軸で判断し採用 |
| COL-06 `ToImmutable` vs `MoveToImmutable`(256 要素)の時間 | 203 vs 171 ns、**信頼区間非重複** | コードサイズ 2,035 vs 891 B で別物 | **現在は実差**(MoveToImmutable が速い)。16 要素でも実差(14.3 vs 11.3 ns)、割り当ては常に半減 |
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
| R-10 インスタンス readonly フィールド | 0.006〜0.016 ns で測定不能 | 読み出しは**オフセット以外同一**(4 B) | ❌ **差なし**(インスタンス readonly は JIT 最適化に寄与しない) |
| R-14 可変長コピーの CopyBlockUnaligned 置換 | 512 B 以上で 0.92〜1.01 倍、信頼区間重複 | 呼び出し形は異なるが**同じ Memmove に到達** | ➖ **誤差**(大サイズ)。16 B では実差(0.81 倍、呼び出し形のオーバーヘッド差)があるが安全性で不採用 |

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
| ⑥ | MEM-04 サイズ依存性の再検証 | in vs 値渡しを 16 / 128 / 256 バイトでも計測 — 呼び出しコストを超えてコピーが表面化するサイズはあるか | MEM-04 | 🔬 次回計測待ち |
| ⑥ | R-14 復活検証(定数長コピー) | CopyBlockUnaligned vs Span.CopyTo を定数 8 / 16 / 64 B で計測 — 長さ保証つき生成コード向けに、実差はどのサイズまで残るか | LAB-CopyBlockUnaligned | 🔬 次回計測待ち |
| ⑥ | R-08 復活検証(大きな Frozen 表) | FrozenDictionary の構築/検索を string キー 1024 件でも計測 — 構築コストを償却できる規模で検索側が勝ち始めるか | COL-02 | 🔬 次回計測待ち |
| ⑤ | System.IO.Pipelines | PipeReader/PipeWriter による I/O パイプライン。Stream 直接処理との比較 | BUF-02 | ✅ 条件付き収録([ASY-03](../README.ja.md#-asy-03-systemiopipelines)、小データは 1.63 倍・アロケーション 1/80。64KB デッドロック注意) |
| ⑤ | IAsyncEnumerable のコスト | await foreach の要素あたりオーバーヘッド(vs IEnumerable / Channel)、\[EnumeratorCancellation\] の作法 | SEQ-03 | ✅ 収録([ASY-04](../README.ja.md#-asy-04-iasyncenumerable-のコスト認知と使い分け)、要素あたり 11.6 倍のコスト認知) |

---
