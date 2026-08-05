# dotnet-performance

**日本語** | [English](README.md)

高速・低アロケーションにチューニングされた .NET ライブラリを実装するためのノウハウ集。

この README が主知識(パターンの分類・一覧・各パターンの解説)の単一ソースであり、これ 1 つで実装判断が完結するように構成している。ライブラリ開発時に AI へ本リポジトリを参照させることで、高性能かつ AOT 対応の実装を再現できる状態にすることを目的とする。

## 🧭 本書の読み方

- パターンには一意の ID(例: MEM-01)を付ける。実装例・テスト・ベンチマーク・測定結果はこの ID で対応付ける
- コード例の ✅ は推奨形、❌ は避ける形
- 実測値は環境・ランタイム世代で変動する。採用時は対象環境で再計測する
- AOT 対応マーク: ✅ そのまま動作 / ⚠️ 実装方法により非互換(各パターンの注記参照) / ❌ AOT では動作しない
- 本書の低レベル最適化はリフレクション・動的コード生成に依存しないため、ほぼすべて AOT 対応。AOT 固有の問題と対策は [aot-compatibility.md](docs/aot-compatibility.md) にまとめる

---

## 🗂️ カテゴリ構成

| カテゴリ | 内容 |
|---|---|
| 💾 MEM | メモリアクセス最適化(境界チェック除去・データレイアウト) |
| 🥞 STK | スタック活用・ゼロアロケーション型設計 |
| 🧺 BUF | バッファ管理・プーリング |
| ⚙️ JIT | JIT 最適化支援(インライン化・分岐除去・特殊化) |
| 🚦 DSP | 呼び出し抽象化・ディスパッチ |
| 🏷️ TYP | 型システム活用(型ディスパッチ・比較・内部アクセス) |
| 🔢 BIT | ビット演算・ブランチレス最適化 |
| 🧮 VEC | SIMD・ベクトル化 |
| 📜 SEQ | 逐次読み書き・シーケンス処理 |
| 🗃️ COL | コレクション最適化 |
| 🔤 TXT | 文字列・フォーマット |
| 🔄 ASY | 非同期 |
| 🔒 CON | 並行・同期 |
| 🖥️ SYS | システム・OS 機能 |
| 🗄️ DAT | データアクセス |
| 🏭 GEN | コード生成 |

## 📋 パターン一覧(サマリー)

> 🔬 は、現在の計測(net10 / x86-64-v4)では文書化された結論が支持されない項目。比較の向きや推奨自体が変わっており、本文の見直しが必要。

| ID | パターン | 目的 | AOT | 実装例 |
|---|---|---|:---:|:---:|
| [MEM-01](#-mem-01-skiplocalsinit) | SkipLocalsInit | ローカル変数ゼロ初期化のスキップ | ✅ | [検証済](benchmarks/results/MEM-01-SkipLocalsInit.md) |
| [MEM-02](#-mem-02-struct-要素配列--ref-アクセスデータ指向レイアウト) | struct 要素配列 + ref アクセス | 要素ごとのヒープ確保と間接参照の排除 | ✅ | [実装](src/PerformancePatterns/Typ/TypeMap.cs) |
| 🔬 [MEM-03](#-mem-03-sliceoffset-length-による明示的スライス) | Slice(offset, length) 明示スライス | 範囲演算子より高速なスライス | ✅ | [検証済](benchmarks/results/MEM-03-SliceStyle.md) |
| 🔬 [MEM-04](#-mem-04-構造体引数の-in--ref-渡し戦略) | 構造体引数の in / ref 渡し | 大きな構造体の値コピー回避 | ✅ | [検証済](benchmarks/results/MEM-04-StructPass.md) |
| [STK-01](#-stk-01-ref-structスタック専用型) | ref struct(スタック専用型) | ヒープエスケープの型レベル禁止 | ✅ | [実装](src/PerformancePatterns/Txt/ValueStringBuilder.cs) |
| [STK-02](#-stk-02-spant--readonlyspant-によるゼロコピーアクセス) | Span\<T\> / ReadOnlySpan\<T\> | ゼロコピーの型付きビュー | ✅ | [実装](src/PerformancePatterns/Seq/SpanTokenizer.cs) |
| [STK-03](#-stk-03-struct-iterator-パターン) | struct iterator パターン | foreach の仮想呼び出し・ヒープ確保除去 | ✅ | [実装](src/PerformancePatterns/Seq/BatchExtensions.cs) |
| [STK-04](#-stk-04-static-ローカルメソッドによる-iterator-の最適化) | static ローカルメソッド iterator | 即時バリデーション + クロージャ防止 | ✅ | [検証済](benchmarks/results/STK-04-LocalFunctionClosure.md) |
| [STK-05](#-stk-05-ボックス化回避と頻出値キャッシュ) | ボックス化回避と頻出値キャッシュ | object 境界のアロケーション排除 | ✅ | [検証済](benchmarks/results/STK-05-BoxingCache.md) |
| [STK-06](#-stk-06-定数サイズ-stackalloc) | 定数サイズ stackalloc | localloc 回避とゼロ初期化制御 | ✅ | [検証済](benchmarks/results/STK-06-StackallocSize.md) |
| [STK-07](#-stk-07-遅延アロケーションと共有シングルトン) | 遅延アロケーションと共有シングルトン | 使うまで確保しない・空を共有する | ✅ | [検証済](benchmarks/results/STK-07-LazyAllocation.md) |
| [STK-08](#-stk-08-inlinearray-による構造体内固定長バッファ) | InlineArray | 構造体内固定長バッファ(.NET 8+) | ✅ | [検証済](benchmarks/results/STK-08-InlineArray.md) |
| [STK-09](#-stk-09-params-readonlyspant) | params ReadOnlySpan\<T\> | 可変長引数の配列確保除去(C# 13) | ✅ | [検証済](benchmarks/results/STK-09-ParamsSpan.md) |
| [BUF-01](#-buf-01-arraypoolt-によるバッファ再利用) | ArrayPool\<T\> | 使い捨てバッファの GC 圧力削減 | ✅ | [実装](src/PerformancePatterns/Buf/TemporaryBuffer.cs) |
| [BUF-02](#-buf-02-ibufferwritert--getspan--advance-パターン) | IBufferWriter\<T\> + GetSpan / Advance | 出力バッファへの直接書き込み | ✅ | [実装](src/PerformancePatterns/Buf/PooledBufferWriter.cs) |
| [BUF-03](#-buf-03-bufferwriterslimtスタックファースト書き込み) | BufferWriterSlim\<T\> | スタックファーストのバッファ書き込み | ✅ | [実装](src/PerformancePatterns/Buf/BufferWriterSlim.cs) |
| [BUF-04](#-buf-04-memoryownertスコープ付きバッファ所有権) | MemoryOwner\<T\> | プールレンタルへの RAII スコープ付与 | ✅ | [実装](src/PerformancePatterns/Buf/MemoryOwner.cs) |
| [BUF-05](#-buf-05-一時バッファの段階戦略stackalloc--arraypool-統合) | 一時バッファの段階戦略 | stackalloc/プールの閾値切替統合 | ✅ | [実装](src/PerformancePatterns/Buf/TemporaryBuffer.cs) |
| [BUF-06](#-buf-06-gcallocateuninitializedarray-によるゼロ初期化スキップ) | GC.AllocateUninitializedArray | 大配列確保のゼロ初期化スキップ | ✅ | [検証済](benchmarks/results/BUF-06-UninitializedArray.md) |
| [BUF-07](#-buf-07-objectpool-による参照型インスタンスの再利用) | ObjectPool | 参照型インスタンスの再利用 | ✅ | [検証済](benchmarks/results/BUF-07-ObjectPool.md) |
| [JIT-01](#️-jit-01-aggressiveinlining--aggressiveoptimization) | AggressiveInlining / AggressiveOptimization | インライン展開・最適化の強制 | ✅ | [検証済](benchmarks/results/JIT-01-Inlining.md) |
| [JIT-02](#️-jit-02-iequatablet-制約による分岐除去) | IEquatable\<T\> 制約による分岐除去 | 比較の仮想ディスパッチ除去 | ✅ | [検証済](benchmarks/results/TYP-02-BitwiseComparer.md) |
| [JIT-03](#️-jit-03-typeoft-分岐によるジェネリック特殊化) | typeof(T) 分岐特殊化 | ジェネリック変換の分岐除去 | ✅ | [検証済](benchmarks/results/JIT-03-TypeofBranch.md) |
| [JIT-04](#️-jit-04-コールドパス分離throw-ヘルパー--grow-の-noinlining) | コールドパス分離 | ホットパスのインライン化促進 | ✅ | [実装](src/PerformancePatterns/Buf/BufferWriterSlim.cs) |
| [JIT-05](#️-jit-05-isreferenceorcontainsreferences-による処理スキップ) | IsReferenceOrContainsReferences 分岐 | 参照なし型の後始末スキップ | ✅ | [検証済](benchmarks/results/JIT-05-ReferenceContainsBranch.md) |
| 🔬 [DSP-01](#-dsp-01-sealed-による-devirtualization) | sealed による devirtualization | 仮想呼び出しの直接化 | ✅ | [検証済](benchmarks/results/DSP-01-SealedDevirt.md) |
| [DSP-02](#-dsp-02-呼び出し抽象化の選択指針) | 呼び出し抽象化の選択指針 | delegate/interface/関数ポインタの使い分け | ✅ | [検証済](benchmarks/results/DSP-02-CallAbstraction.md) |
| [DSP-03](#-dsp-03-ハンドラ列の不変配列化マルチキャストデリゲート回避) | ハンドラ列の不変配列化 | マルチキャストデリゲートの劣化回避 | ✅ | [実装](src/PerformancePatterns/Dsp/HandlerList.cs) |
| [DSP-04](#-dsp-04-static-ラムダの徹底tstate-引き回し) | static ラムダの徹底 | キャプチャ禁止を既定にし状態は TState で渡す | ✅ | [検証済](benchmarks/results/DSP-04-StaticLambda.md) |
| [DSP-05](#-dsp-05-デリゲートパイプラインの事前確定) | デリゲート・パイプラインの事前確定 | 実行時の合成・分岐解決を初期化時へ | ✅ | [検証済](benchmarks/results/DSP-05-PipelineCompose.md) |
| [TYP-01](#️-typ-01-静的型スロットtypemap--typeslot) | 静的型スロット(TypeMap / TypeSlot) | Type キー辞書の配列アクセス化 | ⚠️ | [実装](src/PerformancePatterns/Typ/TypeMap.cs) |
| [TYP-02](#️-typ-02-bitwisecomparert生バイト比較) | BitwiseComparer\<T\> | unmanaged 値型の生バイト比較 | ✅ | [実装](src/PerformancePatterns/Typ/BitwiseComparer.cs) |
| [TYP-03](#️-typ-03-unsafeaccessor非公開メンバーへの直接アクセス) | UnsafeAccessor | 非公開メンバーへの直接アクセス | ✅ | [検証済](benchmarks/results/TYP-03-UnsafeAccessor.md) |
| [TYP-04](#️-typ-04-ジェネリック-static-クラスによる型別キャッシュ) | ジェネリック static 型別キャッシュ | 型ごとの成果物の辞書レス取得 | ✅ | [実装](src/PerformancePatterns/Typ/TypeSlot.cs) |
| [TYP-05](#️-typ-05-unsafeas-による型チェック省略キャスト) | Unsafe.As キャスト | 型保証済みキャストの高速化 | ✅ | [検証済](benchmarks/results/TYP-05-UnsafeAsCast.md) |
| [TYP-06](#️-typ-06-型別成果物の静的事前組み立て) | 型別成果物の静的事前組み立て | 型ごとの文字列・SQL を初期化時に確定 | ✅ | [検証済](benchmarks/results/TYP-06-StaticArtifact.md) |
| [BIT-01](#-bit-01-ドメイン制約を活かした軽量ハッシュ生成) | ドメイン制約を活かした軽量ハッシュ | 既知キー集合の O(1) ハッシュ | ✅ | [実装](src/PerformancePatterns/Col/SampledNameTable.cs) |
| [BIT-02](#-bit-02-2-の累乗サイズ--マスクによる剰余置換) | 2 の累乗サイズ + マスク | 剰余(除算)のビット AND 化 | ✅ | [検証済](benchmarks/results/BIT-02-PowerOfTwoMask.md) |
| [BIT-03](#-bit-03-bitoperations-によるビット走査計数) | BitOperations | ビット走査・計数のハードウェア命令化 | ✅ | [検証済](benchmarks/results/BIT-03-BitOperations.md) |
| [BIT-04](#-bit-04-xxhash3-による汎用ハッシュ) | XxHash3 | 非暗号ハッシュの高速化 | ✅ | [検証済](benchmarks/results/BIT-04-XxHash3.md) |
| 🔬 [VEC-01](#-vec-01-明示的-simdvectort--vector256) | 明示的 SIMD | Vector\<T\> / Vector256 による一括処理 | ✅ | [検証済](benchmarks/results/VEC-01-VectorSum.md) |
| 🔬 [SEQ-01](#-seq-01-spantokenizert) | SpanTokenizer\<T\> | 汎用スパン分割(ゼロアロケーション) | ✅ | [実装](src/PerformancePatterns/Seq/SpanTokenizer.cs) |
| [SEQ-02](#-seq-02-stream-構造体-io) | Stream 構造体 I/O | 構造体の直接バイナリ読み書き | ✅ | [検証済](benchmarks/results/SEQ-02-StructStreamIo.md) |
| [SEQ-03](#-seq-03-遅延評価シーケンス処理batch--segment--traverse) | Batch / Segment / Traverse | 低アロケーションのシーケンス処理 | ✅ | [実装](src/PerformancePatterns/Seq/BatchExtensions.cs) |
| [SEQ-04](#-seq-04-リングバッファ--増分デリミタ探索) | リングバッファ + 増分探索 | ストリーミング受信の分割 | ✅ | [検証済](benchmarks/results/SEQ-04-RingSplit.md) |
| [COL-01](#️-col-01-collectionsmarshal-による内部直接アクセス) | CollectionsMarshal | List/Dictionary 内部への直接アクセス | ✅ | [検証済](benchmarks/results/COL-01-CollectionsMarshal.md) |
| [COL-02](#️-col-02-frozendictionary-の条件付き採用) | FrozenDictionary 条件付き採用 | 不変辞書の検索高速化 | ✅ | [検証済](benchmarks/results/COL-02-FrozenCondition.md) |
| [COL-03](#️-col-03-getalternatelookup-による-span-キー検索) | GetAlternateLookup | Span キーでの辞書検索 | ✅ | [検証済](benchmarks/results/COL-04-SampledNameTable.md) |
| [COL-04](#️-col-04-少数要素ルックアップの戦略選択) | 少数要素ルックアップ戦略 | 規模・形状に応じた実装選択 | ✅ | [実装](src/PerformancePatterns/Col/SampledNameTable.cs) |
| 🔬 [COL-05](#️-col-05-ienumerable-引数の具象型ディスパッチ) | IEnumerable 具象型ディスパッチ | List/配列入力の Span パス化 | ✅ | [検証済](benchmarks/results/COL-05-EnumerableDispatch.md) |
| [COL-06](#️-col-06-コレクション変換の形状特化) | コレクション変換の形状特化 | 生成先の確保・コピー戦略の最適化 | ✅ | [検証済](benchmarks/results/COL-06-CollectionConvert.md) |
| [TXT-01](#-txt-01-ルックアップテーブルによる整形変換) | ルックアップテーブル整形 | 固定書式整形のテーブル化 | ✅ | [実装](src/PerformancePatterns/Txt/Utf8DateTimeFormatter.cs) |
| [TXT-02](#-txt-02-文字列構築の-stackalloc-ファースト化) | 文字列構築の stackalloc ファースト | StringBuilder 代替の低アロケーション構築 | ✅ | [実装](src/PerformancePatterns/Txt/ValueStringBuilder.cs) |
| [TXT-03](#-txt-03-try-パターンによる例外回避) | Try パターン | 例外を制御フローに使わない | ✅ | [検証済](benchmarks/results/TXT-03-TryPattern.md) |
| [TXT-04](#-txt-04-バイト列トークンの直接判定) | バイト列トークン直接判定 | string 化せず u8/uint で判定 | ✅ | [検証済](benchmarks/results/TXT-04-TokenMatch.md) |
| [TXT-05](#-txt-05-utf8trywrite-による-utf-8-直接整形) | Utf8.TryWrite | UTF-8 補間の Span 直接書き込み | ✅ | [検証済](benchmarks/results/TXT-05-Utf8TryWrite.md) |
| [TXT-06](#-txt-06-ascii-特化比較) | ASCII 特化比較 | Ascii クラスによる大小無視処理 | ✅ | [検証済](benchmarks/results/TXT-06-Ascii.md) |
| [TXT-07](#-txt-07-stringcreate--tryformat--ispanformattable) | string.Create / TryFormat | 文字列生成のゼロアロケーション化 | ✅ | [検証済](benchmarks/results/TXT-07-StringCreate.md) |
| [TXT-08](#-txt-08-searchvaluest) | SearchValues\<T\> | 多数候補探索の SIMD 最適化 | ✅ | [検証済](benchmarks/results/TXT-08-SearchValues.md) |
| [TXT-09](#-txt-09-固定長整形の応用イディオム) | 固定長整形の応用 | TryFormat + Fill・ベクトル化トリム | ✅ | [検証済](benchmarks/results/TXT-09-FixedFieldFormat.md) |
| [ASY-01](#-asy-01-async-ステートマシンの省略) | async ステートマシンの省略 | 単純フォワードの Task 直接返し | ✅ | [検証済](benchmarks/results/ASY-01-AsyncElision.md) |
| [ASY-02](#-asy-02-systemthreadingchannels-による生産者消費者) | System.Threading.Channels | 生産者消費者キュー | ✅ | [検証済](benchmarks/results/ASY-02-Channels.md) |
| [ASY-03](#-asy-03-systemiopipelines) | System.IO.Pipelines | I/O ストリーミングのパイプ化 | ✅ | [検証済](benchmarks/results/ASY-03-Pipelines.md) |
| [ASY-04](#-asy-04-iasyncenumerable-のコスト認知と使い分け) | IAsyncEnumerable の使い分け | await foreach の要素あたりコスト | ✅ | [検証済](benchmarks/results/ASY-04-AsyncEnumerable.md) |
| [ASY-05](#-asy-05-valuetask--ivaluetasksource) | ValueTask / IValueTaskSource | 非同期完了パスのアロケーション削減 | ✅ | [検証済](benchmarks/results/ASY-05-ValueTask.md) |
| [ASY-06](#-asy-06-単一ループ型スケジューラ) | 単一ループ型スケジューラ | タイマー乱立の回避 | ✅ | [検証済](benchmarks/results/ASY-06-SchedulerPrimitive.md) |
| [ASY-07](#-asy-07-ストリーミング-io) | ストリーミング I/O | 全体バッファリングの回避 | ✅ | [検証済](benchmarks/results/ASY-07-StreamBuffering.md) |
| [CON-01](#-con-01-interlocked-によるワンショットガード) | Interlocked ワンショットガード | Dispose・初期化のロックレス 1 回実行 | ✅ | [検証済](benchmarks/results/CON-01-DisposeGuard.md) |
| [SYS-01](#️-sys-01-低コストの時刻経過時間取得) | 低コスト時刻取得 | DateTime.UtcNow 回避 | ✅ | [検証済](benchmarks/results/SYS-01-Timestamp.md) |
| [DAT-01](#️-dat-01-db-アクセスの列解決最適化) | DB アクセスの列解決最適化 | 序数キャッシュ・1 パス列解決 | ✅ | [検証済](benchmarks/results/DAT-01-OrdinalResolve.md) |
| [GEN-01](#-gen-01-emit-生成コードの高速化戦略) | Emit 生成コードの高速化戦略 | 生成デリゲートのインライン展開等 | ❌ | [検証済](benchmarks/results/GEN-01-EmitStrategy.md) |
| [GEN-02](#-gen-02-source-generator-生成コードの設計) | Source Generator 生成コードの設計 | 何を生成すれば速いかの指針集 | ✅ | [指針集](docs/generated-code-patterns.md) |

## 💾 MEM: メモリアクセス最適化

### 💾 MEM-01: SkipLocalsInit

**目的:** ローカル変数のゼロ初期化(`.locals init`)をスキップする。

**効果:**

- スタックフレーム確保時の `memset` を除去
- `stackalloc` を多用するメソッドで特に有効
- 実測例: 定数 512 バイトの stackalloc を含むメソッドで 6.6ns → 1.6ns(STK-06 の検証で確認)

**AOT:** ✅ 問題なし

**実装例:**

```csharp
[SkipLocalsInit]
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public bool MoveNext()
{
    Span<byte> localBuffer = stackalloc byte[64]; // ゼロ初期化されない
    // ...
}
```

**ユースケース:** `MoveNext()` のような非常に高頻度な呼び出しメソッド。

**実測結果(net10 / x86-64-v4、stackalloc byte[4096] を含むメソッド呼び出し):** ゼロ初期化あり 19.1 ns → `[SkipLocalsInit]` で **1.6 ns(0.09 倍 = 約 11 倍)**、コードサイズも 604 B → 177 B(memset 経路が消える)。コストは stackalloc のサイズに比例する(使用長ではなく確保長)。→ [測定結果](benchmarks/results/MEM-01-SkipLocalsInit.md)

**注意:**

- プロジェクトに `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` が必要(unsafe コードを書かなくても属性の使用に必要)
- 未初期化領域を読まないよう、書き込み前の読み取りがないことを保証する。`Unsafe.SkipInit(out value)` との併用も検討

---

### 💾 MEM-02: struct 要素配列 + ref アクセス(データ指向レイアウト)

**目的:** エントリ列を class の配列ではなく struct の配列で持ち、`ref` で操作することで、要素ごとのヒープ確保とポインタ追跡を同時に排除する。

**効果:**

- 実測例: class 要素の生成+走査 63.6ns / 664B に対し、struct 要素 + プール配列は 9.8ns / 0B(16 要素)。要素数を増やしてもコストがほぼ一定
- 走査中心の処理でも約 1.5 倍 + アロケーションゼロ(要素が連続配置されキャッシュ効率が上がる)

**AOT:** ✅ 問題なし

**実装例:**

```csharp
private Entry[] entries; // Entry は struct

for (var i = 0; i < entries.Length; i++)
{
    ref var entry = ref entries[i];   // コピーせず直接操作
    entry.Value = Compute(entry.Key);
}
```

**ユースケース:** ハッシュ表のエントリ、パーサーのトークン列、カラムメタデータなど、ライブラリ内部のデータ構造全般。

**注意:**

- `ref var` で受けないと構造体コピーが発生して逆効果になりうる
- class 要素のまま ArrayPool だけ導入しても効果はない(要素個別の確保が残る)。struct 化とセットで初めて効く
- 発展形: ハッシュ表で先頭要素を struct スロットにインライン格納し、溢れのみ別領域(同一配列のストライド先など)に置くフラットレイアウト。ポインタ追跡をさらに削減できるが効果は数 % 規模で、衝突時アクセスで効く

**リポジトリ内実装:** [TypeMap.cs](src/PerformancePatterns/Typ/TypeMap.cs) の `Entry[]`(struct 要素配列 + copy-on-write)

**実測結果(net10 / x86-64-v4、16 バイト要素 × 1024 の走査):** struct + ref アクセス 412.9 ns ≒ struct コピーアクセス 414.7 ns ≒ class 配列 401.6 ns(全て約 3% 以内 — **連続確保直後の class は局所性が崩れておらず、16 バイトのコピーも実質タダ**)。この形の実測に出ない構造的差: 1024 要素で struct は 16 KB 連続、class は約 40 KB のオブジェクト群 + 8 KB の参照配列で、**ヒープが経年するほど class 側の局所性は劣化する** — 採用理由はこの構造面であってマイクロ計測の時間差ではない。→ [測定結果](benchmarks/results/MEM-02-StructArrayRef.md)

---

### 💾 MEM-03: Slice(offset, length) による明示的スライス

**目的:** Span の切り出しに範囲演算子 `span[offset..]` ではなく `span.Slice(offset, length)` を使い、スライス生成コストを削減する。

**効果:**

- 実測例: 同じ書き込み API でもスライス方法の違いだけで 1.2〜1.5 倍程度の差が出る(繰り返しのバイナリ書き込み)。コードサイズも縮小(137B → 87B)
- 範囲演算子は「残り全部」の長さ計算と検証が入るのに対し、長さ明示の `Slice` は必要な検証のみになる

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// 遅い: 範囲演算子(終端までの長さ計算が入る)
BinaryPrimitives.WriteInt32BigEndian(buffer[(i * 4)..], value);

// 速い: 書き込む長さを明示
BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(i * 4, 4), value);
```

**ユースケース:** シリアライザ・エンコーダのホットループ内のスライス全般。

**実測結果(net10 / x86-64-v4、256 回のスライス + 端点読み):** `Slice(offset, 16)` 106.6 ns vs 範囲演算子 107.0 ns(**信頼区間重複 — 時間差は解像できない**)。生成コードは同一ではなく、範囲演算子側に反復あたり 1 個余分なレジスタ移動が残る(15 vs 14 命令、103 vs 100 B)が、幅の広い OoO コアが吸収する。Slice の方がわずかに引き締まったコードを生成するため、ホットループで選んでも損はない — それ以外は可読性で選ぶ。→ [測定結果](benchmarks/results/MEM-03-SliceStyle.md)

**注意:** 可読性の差はごく小さいため、ホットパスでは `Slice(offset, length)` を既定にしてよい。1 回きりのスライスでは差は誤差レベル。

---

### 💾 MEM-04: 構造体引数の in / ref 渡し戦略

**目的:** 大きな構造体を引数で渡すときの値コピーを避ける。

**効果:**

- 値渡しは構造体サイズぶんのコピーが毎回発生する。レジスタに収まらないサイズ(目安 16 バイト超)で効いてくる
- `in` は読み取り専用の参照渡し。ただし**非 readonly 構造体に `in` を付けると、メンバーアクセスのたびに防御的コピーが発生して逆効果**になる

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// ✅ 大きな構造体は readonly struct にして in で受ける
public readonly struct RenderContext   // 例: 40 バイト
{
    // ...
}

public void Draw(in RenderContext context) { ... }

// ✅ 変更を返す必要があるなら ref
public void Advance(ref Cursor cursor) => cursor.Position++;

// ❌ 非 readonly 構造体への in(メンバーアクセスごとに防御的コピー)
public void Draw(in MutableContext context) => context.Value.Use();
```

**設計指針:**

- 16 バイト以下の小さな構造体は値渡しのままでよい(参照渡しの間接参照コストの方が上回る)
- `in` を使うなら型を `readonly struct` にする。フィールドを持つ struct には `readonly` メンバー修飾も併用する
- 戻り値側も同様に、大きな構造体を返すなら `ref readonly` / `ref` 返しを検討する

**実測結果(net10 / x86-64-v4、非インライン呼び出し):**

| 構造体サイズ | 値渡し | in 渡し | 比率 |
|---:|---:|---:|---|
| 8 バイト | 1.21 ns | 1.21 ns | 0.99 |
| 32 バイト | 1.44 ns | 1.16 ns | **0.81** |
| 64 バイト | 1.24 ns | 1.21 ns | 0.98 |
| in + readonly メンバー | — | 1.20 ns | (基準) |
| in + 非 readonly メンバー | — | 1.88 ns | **1.57(❌ 防御的コピー)** |

時間面の実差が出るのは 32 バイトのみ — 64 バイトまでのコピーは非インライン呼び出し自体のコストにほぼ隠れるため、サイズ比例の利得は期待しない。`in` が遅くなることはなく、コードサイズも縮む(63/79 B vs 76/99 B)ので readonly 構造体の既定として安全。実害があるのは防御的コピーの罠の方: 非 readonly メンバーへの `in` 渡しは 1.57 倍遅く、コードサイズも 109 B → 219 B へ倍増する。→ [測定結果](benchmarks/results/MEM-04-StructPass.md)

**注意:** 効果はサイズ・呼び出し頻度・JIT のインライン化状況で変わる。インライン化されるとコピー自体が消えることもあるため、適用前後で計測する。

---

## 🥞 STK: スタック活用・ゼロアロケーション型設計

### 🥞 STK-01: ref struct(スタック専用型)

**目的:** ヒープへのボックス化・エスケープを型システムレベルで禁止する。

**効果:**

- GC 圧力がゼロ(ヒープに積まれない)
- イテレータやリーダーなど一時的な逐次アクセス型に最適
- `foreach` のダックタイピング(`GetEnumerator()`)と組み合わせて配列相当のコストで反復

**AOT:** ✅ 問題なし

**実装例:**

```csharp
public ref struct SpanTokenizer<T> where T : IEquatable<T>
{
    private readonly ReadOnlySpan<T> span;
    // ...
    public readonly SpanTokenizer<T> GetEnumerator() => this;
    public bool MoveNext() { ... }
    public ReadOnlySpan<T> Current { ... }
}
```

**ユースケース:** パーサー、デシリアライザ、テキスト処理のトークン分割。

**リポジトリ内実装(ref struct の実例):** [ValueStringBuilder.cs](src/PerformancePatterns/Txt/ValueStringBuilder.cs) / [BufferWriterSlim.cs](src/PerformancePatterns/Buf/BufferWriterSlim.cs) / [TemporaryBuffer.cs](src/PerformancePatterns/Buf/TemporaryBuffer.cs) / [SpanTokenizer.cs](src/PerformancePatterns/Seq/SpanTokenizer.cs) / [BatchExtensions.cs](src/PerformancePatterns/Seq/BatchExtensions.cs)(SpanBatch の enumerator)

**注意:**

- フィールドとしてクラスに保持できない、`await` / `yield` をまたげない等の制約がある(C# 13 以降は一部緩和)
- C# 13 からは ref struct のインターフェース実装と `allows ref struct` 制約が使用可能

---

### 🥞 STK-02: Span\<T\> / ReadOnlySpan\<T\> によるゼロコピーアクセス

**目的:** データのコピーを作らずに元バッファへの型付きビューを提供する。

**効果:**

- `string`, `byte[]`, `Memory<T>`, スタック変数など様々なソースをコピーなしで扱える
- 配列スライスや文字列スライスを O(1) で生成

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// 文字列をコピーせずにトークナイズ
foreach (var token in new SpanTokenizer<char>(input.AsSpan(), ','))
{
    ProcessToken(token); // ReadOnlySpan<char> — ゼロアロケーション
}
```

**ユースケース:** CSV/DSV パーサー、プロトコルデシリアライザ、テキスト変換パイプライン。

**設計指針:** ライブラリの公開 API は `string` / `T[]` に加えて `ReadOnlySpan<T>` 受け取りのオーバーロードを提供し、内部処理は Span ベースに統一する。

**リポジトリ内実装:** 本リポジトリの全実装の土台(代表: [SpanTokenizer.cs](src/PerformancePatterns/Seq/SpanTokenizer.cs)(0.30〜0.34 倍)(ゼロコスト抽象) / [SampledNameTable.cs](src/PerformancePatterns/Col/SampledNameTable.cs)(Span キー照合))。個別の実測は各パターンの測定結果を参照

---

### 🥞 STK-03: struct iterator パターン

**目的:** `IEnumerable<T>` の `foreach` ではなく、struct 型のダックタイピングイテレータを使う。

**効果:**

- `IEnumerator<T>` インターフェース経由の仮想呼び出しを除去
- ヒープへのイテレータオブジェクト生成を排除
- `ref struct` と組み合わせるとスタックのみで動作
- 実測例: struct enumerable + struct enumerator(ダックタイピング)は 1.4ns / 0B。enumerable 側を class にすると 2.9ns / 24B、`yield return` 実装は 14.4ns / 56B(約 10 倍)— enumerator だけでなく enumerable 自体の struct 化まで効く

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// 呼び出し側(foreach でダックタイピング発動)
foreach (var line in text.SplitLines())
{
    // SplitLinesEnumerator は ref struct かつ GetEnumerator() を持つ
}
```

**ユースケース:** スパン・テキスト処理、ゲームループ内の反復。

**リポジトリ内実装:** [BatchExtensions.cs](src/PerformancePatterns/Seq/BatchExtensions.cs)(SEQ-03 のチャンク分割を struct enumerator で実装した実例) / [テスト](tests/PerformancePatterns.Tests/Seq/BatchTest.cs) / [測定結果](benchmarks/results/SEQ-03-Batch.md)

**実測結果(net10 / x86-64-v4、SEQ-03 の測定より):** struct enumerator ベースの foreach は `Enumerable.Chunk`(IEnumerator 経由)に対し 0.63〜0.74 倍・割り当てゼロ・コードサイズ 1/12〜1/16。

**注意:** struct enumerator を `IEnumerable<T>` として公開するとボックス化されて効果が消える。struct を直接返す `GetEnumerator()` を公開し、`IEnumerable<T>` 実装が必要な場合は明示的実装で分離する。

---

### 🥞 STK-04: static ローカルメソッドによる iterator の最適化

**目的:** `yield return` を含む iterator メソッドで、**引数の即時バリデーション**と**クロージャアロケーションの防止**を同時に達成する。

**効果:**

- 引数チェックが `foreach` 開始前(列挙開始前)に実行される
- `static` 修飾子でコンパイラが外部スコープの変数キャプチャを禁止し、クロージャオブジェクトの不要なアロケーションを防ぐ
- メソッドのシグネチャ(バリデーション層)と実装(イテレーション層)が明確に分離される

**AOT:** ✅ 問題なし(コンパイラ生成のステートマシンは AOT 互換)

**なぜ必要か:** `yield return` を含むメソッドはコンパイラがステートマシンクラスに変換するため、メソッド呼び出し時点では本体が実行されない(遅延実行)。バリデーションを iterator 内に書くと、`foreach` を開始するまで例外が投げられない。

**実装例:**

```csharp
// ❌ 遅延バリデーション: source が null でも foreach するまで気づかない
public static IEnumerable<IReadOnlyList<T>> Batch<T>(this IEnumerable<T> source, int size)
{
    if (source is null) throw new ArgumentNullException(nameof(source)); // ← 実行されない
    foreach (var item in source)
    {
        yield return ...;
    }
}
```

```csharp
// ✅ 即時バリデーション + static ローカルメソッドパターン
public static IEnumerable<IReadOnlyList<T>> Batch<T>(this IEnumerable<T> source, int size)
{
    ArgumentNullException.ThrowIfNull(source);          // ← 呼び出し時点で即時実行
    ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);

    return BatchIterator(source, size);                 // ← iterator を返すだけ

    static IEnumerable<IReadOnlyList<T>> BatchIterator( // ← static: キャプチャ禁止
        IEnumerable<T> source, int size)
    {
        List<T>? bucket = null;
        foreach (var item in source)
        {
            // ...
            yield return bucket;
        }
    }
}
```

**static の効果(コンパイラレベル):**

| | static なし | static あり |
|---|---|---|
| 外部変数のキャプチャ | 可能(意図しないキャプチャのリスク) | コンパイルエラー |
| 生成クラス | クロージャを持つ場合アロケーション発生 | クロージャなし、アロケーション削減 |
| コードの意図 | 不明確 | 「外部状態に依存しない」ことが明示される |

**ユースケース:** `IEnumerable<T>` を返すすべての拡張メソッド、`yield return` を含む LINQ 系メソッド。

**リポジトリ内実装:** [BatchExtensions.cs](src/PerformancePatterns/Seq/BatchExtensions.cs) の `ThrowIfInvalidSize`(static ローカル throw ヘルパー = 即時バリデーション分離の実例)

**実測結果(net10 / x86-64-v4、デリゲート変換して渡す形):** キャプチャするローカル関数 7.00 ns + 88 B/回、static ローカル関数 + state 引数 15.26 ns / **0 B**。割り当て排除の主張は成立するが、**デリゲートとして渡すホットパスなら static ラムダ + TState(DSP-04: 2.96 ns / 0 B)の方が速い**。static ローカル関数の主戦場は直接呼び出し(インライン化される)と iterator/バリデーション分離であり、キャッシュ済みデリゲート用途では DSP-04 の形を使う。→ [測定結果](benchmarks/results/STK-04-LocalFunctionClosure.md)

---

### 🥞 STK-05: ボックス化回避と頻出値キャッシュ

**目的:** object 境界を通る際のボックス化アロケーションを避ける、または固定化する。

**効果(実測例):**

- ヒープへのボックス化は 1.73ns + 24B。ただしボックスがメソッド内に閉じたまま(エスケープしない)なら、エスケープ解析によりスタック化されほぼゼロ(0.004ns)になる
- 頻出値の事前ボックスキャッシュで実行時アロケーションを排除できる
- enum では非ジェネリック `(T)Enum.Parse(typeof(T), name)` がボックス化で 1.3〜2.1 倍遅く必ずアロケートする — ジェネリック版 `Enum.Parse<T>` / `Enum.TryParse<T>` を使う

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// 頻出値(0 / 1 / -1 / true / false 等)の事前ボックス化
private static readonly object BoxedZero = 0;
private static readonly object BoxedOne = 1;

public static object Box(int value) => value switch
{
    0 => BoxedZero,
    1 => BoxedOne,
    _ => value,
};
```

**ユースケース:** object ベースの旧 API(ADO.NET、旧シリアライザ)との境界、ロガーの状態引数。

**注意:** ジェネリック制約(`where T : struct` + インターフェース制約)で呼び出し全体をボックス化なしに設計できるなら、そちらが根本対策(JIT-02 参照)。

**暗黙ボックス化の主な発生源(レビュー観点):**

- struct の interface 型変数・引数への代入(`IComparer<T> c = myStructComparer`)
- `object` 引数への値型渡し(`string.Format` / `string.Concat` / 旧式ロガー / `ArrayList` 等の非ジェネリック API)
- struct メソッドのデリゲート束縛
- 列挙型・値型の `Enum.Parse`(非ジェネリック版)・`GetHashCode`/`Equals(object)` の既定実装経由の比較
- `params object[]` への値型展開(C# 13 の `params ReadOnlySpan<T>` で回避可能)

**実測結果(net10 / x86-64-v4、-1/0/1 を object[] へ格納):** 直接ボックス化 3.68 ns + 24 B/回、事前キャッシュ switch は **2.54 ns / 0 B(0.69 倍)** — このコアでは時間・割り当ての両面でキャッシュが勝つ。分岐とヒープ確保のどちらが安いかは CPU 依存(ポインタバンプ確保が switch を上回るコアもある)だが、GC 圧の排除はどの環境でも成立するため、常駐・高頻度パスで採用する。エスケープしないボックスは JIT が既にスタック化するため、対象は「エスケープする既知値」のみ。→ [測定結果](benchmarks/results/STK-05-BoxingCache.md)

---

### 🥞 STK-06: 定数サイズ stackalloc

**目的:** stackalloc をコンパイル時定数サイズで確保し(フレーム内固定領域になる)、必要分をスライスして使う。可変サイズ確保は `localloc` 命令になり高コスト。

**効果(実測、net10 / x86-64-v4):**

| 確保形 | SkipLocalsInit あり | ゼロ初期化あり |
|---|---|---|
| 定数 512(+ スライス) | **0.27 ns** | 1.4 ns |
| 可変サイズ 512 | 1.8 ns(約 6 倍) | **6.1 ns(約 4 倍)** |

- 定数サイズならゼロ初期化コストもサイズ固定で予測可能。可変サイズは localloc 自体のコストに加え、ゼロ初期化も遅い形になる — サブナノ秒に収まるのは定数 + SkipLocalsInit の組だけ
- MEM-01(SkipLocalsInit)の除去効果(1.4 → 0.27ns)も同時に実証

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// ✅ 定数で確保して必要分をスライス(BUF-05 の閾値イディオムもこの形)
Span<byte> buffer = stackalloc byte[512];
var span = buffer[..size];

// ❌ 可変サイズの stackalloc(localloc 命令化)
Span<byte> buffer = stackalloc byte[size];
```

**ユースケース:** BUF-03 / BUF-05 / TXT-02 の初期バッファ確保すべて。

**注意:** 定数は 256〜512 バイト程度を目安にし、再帰・ループ内での確保は避ける(スタック消費は呼び出しごと)。

---

### 🥞 STK-07: 遅延アロケーションと共有シングルトン

**目的:** 「多くの場合は使われない」ものを使われるまで確保せず、「中身のない値」は共有インスタンスを返す。オブジェクト生成の固定コストを、実際に必要になったパスだけに寄せる。

**効果:**

- 失敗時にしか使わないエラーリスト、購読されるまで不要な Disposables、エラーが出るまで不要な検証辞書などの確保が、正常系・大量生成シナリオで完全に消える
- 空配列・既定デリゲート・空 EventArgs 等の共有で、頻繁に通るパスの割り当てを固定化できる

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// ✅ 失敗が起きるまでリストを作らない
List<Error>? errors = null;
foreach (var item in items)
{
    if (!Validate(item, out var error))
    {
        (errors ??= []).Add(error);
    }
}

// ✅ 空は共有インスタンスを返す(呼び出し側の null チェックも消える)
public IReadOnlyList<Error> Errors => errors ?? (IReadOnlyList<Error>)Array.Empty<Error>();

// ✅ 既定デリゲート・イベント引数の静的共有
private static readonly Func<bool> AlwaysTrue = static () => true;
private static readonly PropertyChangedEventArgs CountChangedEventArgs = new(nameof(Count));
```

**ユースケース:** Result/検証系のエラー収集、ViewModel 付随オブジェクト(Disposables 等)、通知イベント引数(`PropertyChangedEventArgs` はプロパティ名ごとに static キャッシュ)、Null Object(空実装のシングルトン)。

**実測結果(net10 / x86-64-v4):**

- エラーリスト(失敗率 10%): 遅延確保 46.1 ns vs 先行確保 37.3 ns(1.24 倍 — 失敗が実際に起きる混合パスでは null チェック + 初回 Add の分岐が少し効く)で、失敗がある限り両者とも 216 B。**全件成功パスでは遅延側が 0.57 倍かつ割り当て完全ゼロ**(先行確保は常に 216 B)— 勝ちは割り当ての構造であり、効き幅は失敗パスの稀さで決まる
- 空配列: **net10 では `new int[0]` も実測割り当てゼロ**(ランタイムが空配列を共有化)で、`[]` と `new int[0]` は同一の 12 B 共有参照ロードへコンパイルされる — 時間・割り当て・コードのいずれにも差はない。`[]` / `Array.Empty<T>()` を既定にするのはスタイル・可搬性の判断として不変 → [測定結果](benchmarks/results/STK-07-LazyAllocation.md)

**注意:** 遅延確保フィールドはスレッド安全性が必要なら lock または CON-01 と組み合わせる(単一スレッド前提の型ならそのままでよい)。

---

### 🥞 STK-08: InlineArray による構造体内固定長バッファ

**目的:** 構造体の中に固定長の要素列を「配列オブジェクトなし」で埋め込む(.NET 8+)。

**効果:**

- 従来 `fixed` バッファ(unsafe 必須・unmanaged 型限定)でしか書けなかった構造体内配列を、安全なコードで参照型を含めて表現できる
- 要素は構造体本体に埋め込まれるため、スタック上のローカルやプールされたエントリの中で完結し、別途のヒープ確保が発生しない
- `MemoryMarshal.CreateSpan` / 添字アクセスで Span として扱える

**AOT:** ✅ 問題なし

**実装例:**

```csharp
[InlineArray(8)]
public struct Slot8<T>
{
    private T element0;   // フィールドは 1 つだけ宣言する
}

// 使用側: 添字・foreach・Span 化が可能
var slots = new Slot8<int>();
slots[0] = 1;
Span<int> span = slots;
```

**ユースケース:** 小さな固定長ワーク領域、ハッシュ表エントリのインライン格納(MEM-02 の発展)、状態機械の履歴バッファ。

**実測結果(net10 / x86-64-v4、int×8 の書き込み+合計):** `new int[8]` 4.81 ns / 56 B に対し、stackalloc 2.87 ns(0.60)、InlineArray 2.92 ns(0.61)— いずれもゼロアロケーションで時間は同等(信頼区間重複)。コードサイズは InlineArray の方がわずかに小さい(112 vs 134 B)。InlineArray の価値は「構造体のフィールドとして持てる」ことにある。→ [測定結果](benchmarks/results/STK-08-InlineArray.md)

**注意:** 要素数はコンパイル時定数。可変長には使えないため、超過時は BUF-05 の段階戦略へ切り替える設計にする。

---

### 🥞 STK-09: params ReadOnlySpan\<T\>

**目的:** 可変長引数(`params`)呼び出しのたびに発生する配列確保をなくす(C# 13 / .NET 9)。

**効果:**

- `params T[]` は呼び出しごとにヒープ配列を確保する。`params ReadOnlySpan<T>` はコンパイラがスタック上の一時領域を使うため**アロケーションゼロ**になる
- 呼び出し側のコードは変更不要(既存の `Log("a", "b")` のような呼び出しがそのまま速くなる)
- BCL でも `string.Concat`・`string.Format` 系のオーバーロード追加という形で同じ方針が採られている

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// ❌ 呼び出しごとに配列を確保
public static void Trace(params object[] values) { ... }

// ✅ 配列確保なし(C# 13)。値型は STK-05 のボックス化にも注意
public static void Trace(params ReadOnlySpan<string> values)
{
    foreach (var value in values)
    {
        Write(value);
    }
}
```

**ユースケース:** ログ・診断 API、可変長のキー結合、複数値を受けるユーティリティ。

**実測結果(net10 / x86-64-v4、引数 3 個):** `params T[]` 4.46 ns / 48 B → `params ReadOnlySpan<T>` **1.10 ns / 0 B(0.25 倍)**。呼び出し構文はそのままでアロケーションが消える。→ [測定結果](benchmarks/results/STK-09-ParamsSpan.md)

**注意:** ライブラリの公開 API で `params T[]` から置き換える場合、既存の「配列を明示的に渡す呼び出し」との互換のためオーバーロード併設を検討する。

---

## 🧺 BUF: バッファ管理・プーリング

### 🧺 BUF-01: ArrayPool\<T\> によるバッファ再利用

**目的:** 頻繁に使い捨てられるバッファの GC 圧力を削減する。

**効果:**

- 大きな短命配列(例: シリアライズ用バッファ)を LOH に置かずに済む
- `Rent` / `Return` は非常に低コスト(ほぼロックフリー)

**AOT:** ✅ 問題なし

**実装例:**

```csharp
var buffer = ArrayPool<byte>.Shared.Rent(size);
try
{
    // buffer を使った処理
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}
```

**ユースケース:** ネットワーク I/O バッファ、シリアライズ、一時的なデータ変換。

**注意:**

- レンタルされる配列は要求サイズ以上(通常 2 の累乗)。長さに依存する処理は要求サイズ側でスライスする
- 機密データを扱う場合は `Return(buffer, clearArray: true)` でクリアする
- 返却漏れは GC が回収するため致命的ではないが、プール効率が落ちる。スコープ管理は BUF-04 参照

**リポジトリ内実装(ArrayPool を後背に使う型):** [TemporaryBuffer.cs](src/PerformancePatterns/Buf/TemporaryBuffer.cs)(BUF-05) / [MemoryOwner.cs](src/PerformancePatterns/Buf/MemoryOwner.cs)(BUF-04) / [BufferWriterSlim.cs](src/PerformancePatterns/Buf/BufferWriterSlim.cs)(BUF-03) / [PooledBufferWriter.cs](src/PerformancePatterns/Buf/PooledBufferWriter.cs)(BUF-02)

**実測結果:** 素の Rent/Return は 4 KB ライフサイクルで割り当てゼロ(`new byte[]` は 4,120 B)。時間は fill 支配でラッパーとの差は誤差 → [BUF-04-MemoryOwner.md](benchmarks/results/BUF-04-MemoryOwner.md)

---

### 🧺 BUF-02: IBufferWriter\<T\> + GetSpan / Advance パターン

**目的:** 書き込み先を抽象化し、ゼロコピーで出力バッファへ直接書き込む。

**効果:**

- 中間配列不要。`GetSpan` で出力先のスライスを直接受け取り、`Advance` で進める
- `PooledBufferWriter`, `PipeWriter`, `ArrayBufferWriter` など様々なバックエンドで使える

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// 型付き値をバッファライターへ直接書き込む
public static void Write<T>(this IBufferWriter<byte> writer, T value)
    where T : unmanaged
{
    var span = writer.GetSpan(Unsafe.SizeOf<T>());
    Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), value);
    writer.Advance(Unsafe.SizeOf<T>());
}
```

**ユースケース:** プロトコルエンコーダ、バイナリシリアライザ。

**設計指針:** シリアライザ系ライブラリの出力 API は `byte[]` 返しではなく `IBufferWriter<byte>` 受け取りを基本形にする。

**リポジトリ内実装:** [PooledBufferWriter.cs](src/PerformancePatterns/Buf/PooledBufferWriter.cs)(ArrayPool 後背 + JIT-04 の Grow 分離 + JIT-05 の型別クリア) / [テスト](tests/PerformancePatterns.Tests/Buf/PooledBufferWriterTest.cs) / [ベンチマーク](benchmarks/PerformancePatterns.Benchmarks/Buf/BufferWriterBenchmark.cs) / [測定結果](benchmarks/results/BUF-02-BufferWriter.md)

**実測結果(net10 / x86-64-v4、16B × 64 チャンク書き込み):** `MemoryStream` + ToArray 比で `ArrayBufferWriter` 0.68 倍 / `PooledBufferWriter` **0.57 倍 — 3 方式中最速**で、かつ**アロケーション 2,976B → 32B(ライター本体のみ)**。繰り返し書き込みでの GC 圧力をゼロ化できる。

---

### 🧺 BUF-03: BufferWriterSlim\<T\>(スタックファースト書き込み)

**目的:** スモールペイロードはスタック、大ペイロードはプールから — ゼロアロケーションファースト設計。

**効果:**

- 初期バッファ(stackalloc)が足りる間は完全にアロケーションフリー
- 超過時のみ `ArrayPool` からレンタルし、初期バッファの内容をコピー

**AOT:** ✅ 問題なし

**実装例:**

```csharp
Span<byte> stack = stackalloc byte[256];
var writer = new BufferWriterSlim<byte>(stack);
writer.Write(someHeader);
// 小さいデータなら stack のみで完結
writer.Dispose(); // もしプールが使われていれば返却
```

**ユースケース:** ログメッセージ組み立て、小さなバイナリパケット生成。

**リポジトリ内実装:** [BufferWriterSlim.cs](src/PerformancePatterns/Buf/BufferWriterSlim.cs) / [テスト](tests/PerformancePatterns.Tests/Buf/BufferWriterSlimTest.cs) / [ベンチマーク](benchmarks/PerformancePatterns.Benchmarks/Buf/BufferWriterSlimBenchmark.cs) / [測定結果](benchmarks/results/BUF-03-BufferWriterSlim.md)

**実測結果(net10 / x86-64-v4、16 バイト × N の書き込みライフサイクル):**

| 方式 | 64 B(スタック内) | 4096 B(成長パス) |
|---|---|---|
| `ArrayBufferWriter`(基準) | 25.2 ns / 312 B | 1,427 ns / **8,056 B** |
| PooledBufferWriter(BUF-02) | 24.3 ns / 32 B | 1,328 ns / 32 B |
| **BufferWriterSlim** | **19.0 ns** / **0 B** | **1,283 ns** / **0 B** |

Slim が両軸で勝つ: 64 B で 0.76 倍、成長パスで 0.90 倍(信頼区間非重複)、割り当ては 312 B → 0 / 8,056 B → 0。同期スコープ内なら Slim、`IBufferWriter<T>` として渡す・フィールドに保持するなら BUF-02 を選ぶ。

**注意:** stackalloc サイズは 256〜512 バイト程度を目安とし、再帰・ループ内での確保は避ける(スタックオーバーフロー対策)。

---

### 🧺 BUF-04: MemoryOwner\<T\>(スコープ付きバッファ所有権)

**目的:** `ArrayPool` レンタルに RAII(using)スコープを付与する。

**効果:**

- `Dispose` 漏れを型システムで防げる(`using` 強制可能)
- 要求長と実際のレンタル長(常に 2 の累乗)の差を隠蔽し、正確な `Span` / `Memory` を提供

**AOT:** ✅ 問題なし

**実装例:**

```csharp
using var owner = MemoryOwner<byte>.Allocate(requestedSize);
await socket.ReceiveAsync(owner.Memory, cancel);
ParsePacket(owner.Span);
// } ← Dispose で自動返却
```

**ユースケース:** 非同期 I/O バッファ、ファイル読み込み、プロトコル受信バッファ。

**リポジトリ内実装:** [MemoryOwner.cs](src/PerformancePatterns/Buf/MemoryOwner.cs)(`IMemoryOwner<T>` 準拠、二重 Dispose は CON-01 の Interlocked ガード) / [テスト](tests/PerformancePatterns.Tests/Buf/MemoryOwnerTest.cs) / [ベンチマーク](benchmarks/PerformancePatterns.Benchmarks/Buf/MemoryOwnerBenchmark.cs) / [測定結果](benchmarks/results/BUF-04-MemoryOwner.md)

**実測結果(net10 / x86-64-v4、4 KB の取得→書き込み→集計→解放):** 時間は fill+sum が支配的で 1.63〜1.65 μs に収まり、**MemoryOwner と素の Rent/Return の差は範囲が重なり分解できない(➖誤差 = ラッパーコストは計測分解能以下)**。割り当ては `new byte[]` 4,120 B / 素の ArrayPool 0 B / **MemoryOwner 32 B(所有オブジェクトのみ)** / TemporaryBuffer 0 B。価値は using 強制・正確な長さ・二重 Dispose 安全という設計面にある。同期スコープ内なら BUF-05(TemporaryBuffer)、非同期境界をまたぐなら本型。

**補足:** `IMemoryOwner<T>` インターフェースに準拠させると `MemoryPool<T>` 系 API と相互運用できる。非同期メソッドをまたぐ場合は ref struct にできないため class または struct で実装する。

---

### 🧺 BUF-05: 一時バッファの段階戦略(stackalloc / ArrayPool 統合)

**目的:** 一時バッファの確保を「小さければ stackalloc、大きければ ArrayPool」の閾値切替に統一し、ref struct でスコープ管理する。

**効果:** 大多数の呼び出し(小サイズ)が完全アロケーションフリーになり、大サイズも GC 圧力ゼロになる。BUF-03(書き込み特化)・TXT-02(文字列特化)の汎用形。

**AOT:** ✅ 問題なし

**実装例:**

```csharp
public ref struct TemporaryBuffer<T>
{
    private T[]? pooled;

    public TemporaryBuffer(Span<T> initial, int length)
    {
        Span = initial[..length];
    }

    public TemporaryBuffer(int length)
    {
        pooled = ArrayPool<T>.Shared.Rent(length);
        Span = pooled.AsSpan(0, length);
    }

    public Span<T> Span { get; }

    public void Dispose()
    {
        var toReturn = pooled;
        if (toReturn is not null)
        {
            pooled = null;
            ArrayPool<T>.Shared.Return(toReturn);
        }
    }
}

// 呼び出し側: 閾値で stackalloc とプールを切り替え(どちらの経路でも Span は要求長ちょうど)
using var buffer = size <= 512
    ? new TemporaryBuffer<char>(stackalloc char[512], size)
    : new TemporaryBuffer<char>(size);
Process(buffer.Span);
```

**ユースケース:** エンコード変換、P/Invoke 用バッファ、一時ワーク領域全般。

**リポジトリ内実装:** [TemporaryBuffer.cs](src/PerformancePatterns/Buf/TemporaryBuffer.cs) / [テスト](tests/PerformancePatterns.Tests/Buf/TemporaryBufferTest.cs) / [ベンチマーク](benchmarks/PerformancePatterns.Benchmarks/Buf/TemporaryBufferBenchmark.cs) / [測定結果](benchmarks/results/BUF-05-TemporaryBuffer.md)

**実測結果(net10 / x86-64-v4):** 4096 要素では `new T[]` 比 0.09 倍(約 11 倍高速、ゼロ初期化コストの除去)+ 0B。64 要素の stackalloc 経路は `new` より僅かに遅い(2.3ns vs 2.0ns)が 88B → 0B — **小サイズの価値は速度ではなく GC 圧力ゼロ化**にある。`ArrayPool` 直接利用と比べると小サイズで有利(stackalloc 経路がプールアクセスを回避: 2.3 vs 4.2 ns)。

**注意:**

- 変種として `[ThreadStatic]` static バッファの使い回しがある(完全アロケーションゼロ)が、再入・async 境界をまたぐ保持・スレッドごとのメモリ滞留に注意。ThreadStatic フィールドへのアクセス自体にもコストがあるため、使う場合はループ前にローカル変数へ退避する
- stackalloc 側の閾値は 256〜512 要素程度を目安にする(BUF-03 と同様)

---

### 🧺 BUF-06: GC.AllocateUninitializedArray によるゼロ初期化スキップ

**目的:** ヒープ配列の確保時ゼロ初期化をスキップする(ヒープ版 SkipLocalsInit)。全域を自分で書き潰すことが確実な一時バッファ向け。

**効果(実測、net10 / x86-64-v4、`new byte[N]` 比):**

| サイズ | 比率 | 判定 |
|---|---|---|
| 256B / 2048B | 0.98 / 0.94 | 小サイズでは省けるゼロ初期化が小さすぎて差が出ない |
| 4096B | 0.60 | 有効 |
| 64KB | **0.18(約 5 倍)** | 最も有効な帯域 |
| 1MB(LOH 級) | 0.98 | 確保ごとの GC コストが支配し差が消える |

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// 直後に全域へ書き込むことが確実な受信バッファ等
var buffer = GC.AllocateUninitializedArray<byte>(length);
stream.ReadExactly(buffer);
```

**ユースケース:** 一度きりの大きめバッファ(4KB〜数百 KB)の確保。繰り返し確保する場合は BUF-01(ArrayPool)を優先する。

**注意:**

- 未初期化領域を読まない保証は呼び出し側の責任(読み出し前に必ず全域を書く)
- `GC.AllocateArray(pinned: true)` による POH 確保は通常確保の約 17.5 倍のコスト(実測)。長寿命 I/O バッファの断片化回避専用とし、起動時に一度だけ使う(不採用一覧 R-13 参照)

---

### 🧺 BUF-07: ObjectPool による参照型インスタンスの再利用

**目的:** 構築コストが高い参照型(パーサー状態、ビルダー、コンテキストオブジェクト)を再利用して確保・GC を減らす。

**効果:**

- `ArrayPool`(BUF-01)がバッファ専用なのに対し、任意の参照型インスタンスを対象にできる
- 効くのは「構築コストが実測で有意」かつ「寿命が明確」なものに限られる。単純な小オブジェクトは GC の方が安いことが多い

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// [ThreadStatic] 1 要素プール: 最小構成でスレッド安全(再入時は通常確保にフォールバック)
[ThreadStatic]
private static StringBuilder? cached;

public static StringBuilder Rent()
{
    var builder = cached;
    if (builder is null)
    {
        return new StringBuilder(DefaultCapacity);
    }

    cached = null;   // 取り出し中は null にして再入に備える
    return builder;
}

public static void Return(StringBuilder builder)
{
    // 肥大化したバッファを保持し続けない
    if (builder.Capacity <= MaxRetainedCapacity)
    {
        builder.Clear();
        cached = builder;
    }
}
```

**ユースケース:** ビルダー・コンテキストの使い回し、汎用プール実装を採用する場合の設計指針。

**注意:**

- **返却漏れ・二重返却・返却後使用**は追跡困難なバグになる。`using` スコープで包む(BUF-04 と同じ考え方)
- 保持サイズの上限を設けないと、一度肥大化したインスタンスが常駐する
- 参照型を保持するプールは、返却時に内部参照をクリアしないとオブジェクトの寿命が伸びる

**実測結果(net10 / x86-64-v4、StringBuilder でキー文字列組み立て):** 毎回 `new StringBuilder(256)` 19.97 ns + 648 B に対し、`[ThreadStatic]` 1 要素プールは **13.51 ns + 64 B(0.68 倍、割り当ては結果文字列のみ = 0.10 倍)**。→ [測定結果](benchmarks/results/BUF-07-ObjectPool.md)

---

## ⚙️ JIT: JIT 最適化支援

### ⚙️ JIT-01: AggressiveInlining / AggressiveOptimization

**目的:** JIT にメソッドのインライン展開または最適化を強制指示する。

**効果:**

- `AggressiveInlining`: 関数呼び出しコストをゼロにする。ホットパスのラッパー関数に最適
- `AggressiveOptimization`: Tiered Compilation を回避して最初から最適化コンパイルする

**AOT:** ✅ 問題なし。`AggressiveInlining` は AOT コンパイル時にも有効。`AggressiveOptimization` は AOT には階層型コンパイルがないため実質無意味(無害)

**実装例:**

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static bool TryGetValue<TKey>(...)
{
    // 呼び出し元に展開されコール命令が消える
}
```

**ユースケース:** `TryGetValue`、`Read`、`Write` などの 1〜数命令のヘルパーメソッド。

**注意:**

- `AggressiveInlining` の付けすぎはコードサイズ肥大により命令キャッシュ効率を悪化させうる。小さなホットメソッドに限定する
- `AggressiveOptimization` は .NET 8+ では Dynamic PGO(実行時プロファイルに基づく最適化)を無効化するため、かえって遅くなるケースがある。必ずベンチマークで確認してから使用する

**実測結果(net10 / x86-64-v4、ループ持ちヘルパー × 1024 呼び出し):** NoInlining 1.180 μs に対し既定 0.943 μs / Aggressive 0.959 μs。**インライン化自体の価値は実差**(NoInline は既定と信頼区間非重複で +25%)だが、**既定と Aggressive の呼び出し側コードは 100 B で完全一致** — net10 の既定ポリシー(PGO)はループ持ちヘルパーも既にインライン化しており、**属性はヒューリスティクスが見送る形への保険**と位置づける。→ [測定結果](benchmarks/results/JIT-01-Inlining.md)

---

### ⚙️ JIT-02: IEquatable\<T\> 制約による分岐除去

**目的:** ジェネリック型引数に `IEquatable<T>` 制約を加え、JIT に専用の比較コードを生成させる。

**効果:**

- `EqualityComparer<T>.Default` の仮想ディスパッチが除去される
- プリミティブ型では直接の `==` 命令に展開される
- `IndexOf` 等のスパン検索は型に特化した SIMD 実装が選択される
- struct を制約付きジェネリック(`where TComparer : IComparer<T>` 等)で受けると constrained call になり、ボックス化なしで型別特殊化コードが生成される。interface 型の引数(`IComparer<T> comparer`)で受けると struct 実装は毎回ボックス化される — 「比較子・ストラテジは struct + ジェネリック制約で受ける」が定石

**AOT:** ✅ 問題なし。値型のジェネリックは AOT コンパイル時に型ごとに完全特殊化されるため、JIT と同等の最適化が効く

**実装例:**

```csharp
public ref struct SpanTokenizer<T> where T : IEquatable<T>
{
    public bool MoveNext()
    {
        // T.IndexOf → JIT が型に特化した SIMD 実装を選択
        var index = span[newStart..].IndexOf(separator);
        // ...
    }
}
```

**ユースケース:** コレクション、サーチ、スプリッタなどの汎用アルゴリズム実装。

**実測結果(TYP-02 の測定より):** 16 バイト構造体キーの辞書ルックアップで、`IEquatable<T>` 実装 struct の既定比較子は **5.6 ns / 割り当てゼロ**(未実装 struct は 25.7 ns + 96 B/回のボックス化)。制約による脱仮想化・ボックス化回避の効果そのもの → [TYP-02-BitwiseComparer.md](benchmarks/results/TYP-02-BitwiseComparer.md)

---

### ⚙️ JIT-03: typeof(T) 分岐によるジェネリック特殊化

**目的:** ジェネリックメソッド内に `if (typeof(T) == typeof(int))` の分岐を書き、JIT の定数畳み込みで型ごとの特殊化コードを生成させる。

**効果:**

- `typeof(T)` 比較は JIT がコンパイル時定数として評価し、成立しない分岐をコードごと削除する。分岐を 10 個並べてもコストはほぼ増えない(実測例: 2.20ns vs 2.38ns)
- `Convert.ChangeType` などのボックス化経由の変換(実測例: 3.39ns + 24B)を完全に回避できる

**AOT:** ✅ 問題なし(値型は AOT でも完全特殊化されるため同様に畳み込まれる)

**実装例:**

```csharp
public static T Convert<T>(int value)
{
    if (typeof(T) == typeof(int))
    {
        return Unsafe.As<int, T>(ref value);
    }
    if (typeof(T) == typeof(long))
    {
        var l = (long)value;
        return Unsafe.As<long, T>(ref l);
    }
    // ...
    throw new NotSupportedException();
}
```

**ユースケース:** 型変換層、シリアライザ・フォーマッタのプリミティブ特殊化。

**実測結果(net10 / x86-64-v4、int[1024] の合計):** typeof(T) 分岐つきジェネリック 212.4 ns vs 手書き int 版 213.7 ns — **分岐コストはゼロ**(コードサイズ 35 vs 32 B でほぼ同一。JIT がインスタンス化ごとに `typeof(T) == typeof(int)` を定数へ畳み込み、分岐を除去する)。フォールバック経路の正しさは Verify で確認。→ [測定結果](benchmarks/results/JIT-03-TypeofBranch.md)

**関連する知見:** `typeof(X)` を `static readonly Type` フィールドにキャッシュする最適化は無意味(JIT が `typeof` 自体を定数化するため、実測で速度・コードサイズとも完全に同値)。可読性を優先してよい。

---

### ⚙️ JIT-04: コールドパス分離(Throw ヘルパー / Grow の NoInlining)

**目的:** 例外スロー・バッファ拡張など稀にしか通らないコードを別メソッドへ分離し、ホットパスのコードサイズを小さくしてインライン化を促進する。

**効果:**

- ホットメソッドが小さくなり、JIT のインライン化判断が通りやすくなる
- `throw` を含むメソッドはインライン化されないため、スローをヘルパーに分離するとホット側がインライン可能になる
- BCL の ThrowHelper / `ArgumentNullException.ThrowIfNull` と同じ設計

**AOT:** ✅ 問題なし

**実装例:**

```csharp
public void Append(char c)
{
    if ((uint)length < (uint)buffer.Length)
    {
        buffer[length++] = c;   // ホットパス: 小さく保つ
        return;
    }

    GrowAndAppend(c);           // コールドパス: 分離して非インライン化
}

[MethodImpl(MethodImplOptions.NoInlining)]
private void GrowAndAppend(char c)
{
    Grow();
    Append(c);
}

[DoesNotReturn]
private static void ThrowInvalidState() => throw new InvalidOperationException(...);
```

**ユースケース:** builder/writer の Grow 処理、引数検証、稀なエラーパス全般。

**リポジトリ内実装:** [BufferWriterSlim.cs](src/PerformancePatterns/Buf/BufferWriterSlim.cs) / [ValueStringBuilder.cs](src/PerformancePatterns/Txt/ValueStringBuilder.cs)(いずれも Grow を NoInlining 分離)

**実測結果(net10 / x86-64-v4、単離マイクロ):** 成長処理込みの太い Write(569 B、非インライン)635.1 ns に対し、分離 + AggressiveInlining の Write(ホット側 103 B)は **631.1 ns(0.99 倍)** — 時間は同等でホットメソッドは 5.5 分の 1。Write 1 回あたりの呼び出しコストは小さく、単離計測では分離が時間に出ない。**本パターンの価値は「呼び出し元へのインライン化を可能にし、その先の最適化を解放する」ことにあり、常に速くなる魔法ではない** — 適用は計測とセットで。→ [測定結果](benchmarks/results/JIT-04-ColdPathSplit.md)

---

### ⚙️ JIT-05: IsReferenceOrContainsReferences による処理スキップ

**目的:** 参照を含まない型 `T` に対して、GC 参照解放のための後始末(配列クリア等)を分岐でスキップする。

**効果(実測、net10 / x86-64-v4):**

- `RuntimeHelpers.IsReferenceOrContainsReferences<T>()` は JIT が型ごとに定数畳み込みし、成立しない側の分岐をコードごと削除する
- `int[1024]` のクリア: 無条件 19.0ns → 条件分岐 **0.008ns**(仕事ごと消滅。コードサイズ 510B → 28B)
- 参照型(`string[]`、クリアが必要な側)ではチェックのオーバーヘッドは実測ゼロ(101.5ns vs 102.4ns、コードサイズ同一)

**AOT:** ✅ 問題なし(値型は AOT でも完全特殊化され定数化される)

**実装例:**

```csharp
public void Return(T[] array)
{
    // 参照を含まない型では GC のためのクリアは不要(BCL の ArrayPool/List と同じ判断)
    if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
    {
        Array.Clear(array);
    }

    pool.Push(array);
}
```

**ユースケース:** プール返却時のクリア、コレクションの Clear/Remove、シリアライザのバッファ後始末、コピー/比較方式の型別切替。

**注意:** スキップしてよいのは「GC に参照を解放させる」目的のクリアだけ。機密データ消去などセキュリティ目的のクリアは型にかかわらず必ず実行する。

---

## 🚦 DSP: 呼び出し抽象化・ディスパッチ

### 🚦 DSP-01: sealed による devirtualization

**目的:** 実装クラスを `sealed` にして、JIT が仮想呼び出し・インターフェース呼び出しを直接呼び出し+インライン化へ置き換え(devirtualization)できるようにする。

**効果:**

- sealed 型の変数経由の呼び出しは実行時型が確定するため、JIT が直接呼び出しに落とせる
- 効果はコンテキスト依存(既にインライン化やガード付き devirtualization が効いている場合は差が出ないことも実測されている)が、コストはゼロ

**AOT:** ✅ 問題なし。AOT には実行時プロファイルによるガード付き devirtualization がないため、静的に sealed で確定させる価値がむしろ大きい

**実装例:**

```csharp
public sealed class BinaryFormatter : IFormatter { ... }
```

**実測結果(net10 / x86-64-v4、単一実装のインターフェース経由呼び出し × 1024):**

| 保持形 | 時間 | 比率 |
|---|---:|---|
| インターフェース参照(非 sealed 実装) | 220.7 ns | 1.00 |
| インターフェース参照(sealed 実装) | 221.9 ns | 1.01(➖誤差、コードサイズ 84 B で同一) |
| **具象 sealed 型の参照** | 215.2 ns | **0.98**(27 B、直接呼び出し + インライン化) |

**net10 ではインターフェース参照越しの呼び出しは sealed でも速くならなかった**(生成コードサイズ一致)。分岐予測の効いた単相インターフェース呼び出し自体がほぼ無料で、具象 sealed 型で持っても時間は約 2% しか縮まない。具象型保持の実利は**コードサイズ(27 B vs 84 B)とインライン化の余地**にあり、AOT / 動的 PGO なし環境で効く。sealed 自体はコストゼロなので既定にする方針は不変。→ [測定結果](benchmarks/results/DSP-01-SealedDevirt.md)

**設計指針:** 継承を設計意図として明示的に許すクラス以外、ライブラリの実装クラスはすべて sealed を既定とする(BCL も同方針)。

---

### 🚦 DSP-02: 呼び出し抽象化の選択指針

**目的:** コールバック・ファクトリ・ストラテジの保持形態(デリゲート / インターフェース / 関数ポインタ)を、実測に基づいて選択する。

**知見(実測例):**

- 最近のランタイム(.NET 9/10)では、インターフェース / abstract 経由の呼び出しはデリゲート呼び出しと同等〜高速(100 万回で 197μs vs 227μs)。「デリゲートの方が軽い」という古い常識は成立しない
- static メソッドを直接バインドしたデリゲートは最も遅い形態になりうる(this 引数を詰め替える thunk を経由するため)。デリゲートに乗せるなら、コンパイラがキャッシュするラムダ(`static (x) => Foo(x)` 形式)の方が速いことがある
- メソッド内の小さな処理はラムダではなく static ローカル関数にする(実測例: コードサイズ 185B vs 6B、ローカル関数は完全インライン化されデリゲート生成も呼び出しも消える)
- 関数ポインタ `delegate*<T>` はコードサイズこそ小さいが、**net10 では最も遅い保持形態になりうる**(下記実測)。適用はベンチマーク前提

**実測結果(net10 / x86-64-v4、加算 × 1024):**

| 保持形態 | 時間 | 比率 | コードサイズ |
|---|---:|---|---:|
| **具象 sealed 型で保持** | **215.8 ns** | **1.00** | 27 B |
| abstract 基底経由 | 223.6 ns | 1.04 | 81 B |
| インターフェース経由 | 224.3 ns | 1.04 | 84 B |
| デリゲート(static ラムダ) | 254.6 ns | 1.18 | 85 B |
| **関数ポインタ `delegate*`** | **1,250.7 ns** | **5.80(❌ 最遅)** | 42 B |

**関数ポインタが最遅になる理由:** `calli` は JIT がインライン化できず、Dynamic PGO の投機的最適化(推測付き脱仮想化)も効かない。一方デリゲートの `Invoke` は PGO がターゲットを推測してインライン化できるため、**「生ポインタだから速い」は net10 では成立しない**。関数ポインタの用途は相互運用境界・AOT・投機が効かない多相ターゲットであり、速度目的の一般手段ではない。

分岐予測の効いた単相仮想呼び出しはほぼ無料(約 4%)で、デリゲート ≒ abstract ≒ インターフェース — 「デリゲートはインターフェースより重い」という古い常識は成立しない。→ [測定結果](benchmarks/results/DSP-02-CallAbstraction.md)

**AOT:** ✅ 問題なし(マネージド関数ポインタは AOT 対応)

**ユースケース:** DI コンテナのファクトリ表、シリアライザのフォーマッタ解決、パイプラインのステージ保持。

---

### 🚦 DSP-03: ハンドラ列の不変配列化(マルチキャストデリゲート回避)

**目的:** 購読者が複数になりうるイベント/コールバックを、マルチキャストデリゲート(`+=`)ではなく不変配列 + `Volatile.Read` で保持・実行する。

**効果:**

- マルチキャストデリゲートは購読者数にほぼ比例して劣化する。損益分岐は購読 2 個で、購読 4 個では不変配列の foreach が 2.75 倍高速(下記実測)
- 発火側はロック不要(copy-on-write で購読変更時のみ配列を差し替える)

**AOT:** ✅ 問題なし

**実装例:**

```csharp
private readonly object sync = new();
private Action<T>[] handlers = [];

public void Subscribe(Action<T> handler)
{
    lock (sync)
    {
        var current = handlers;
        var next = new Action<T>[current.Length + 1];
        current.CopyTo(next, 0);
        next[^1] = handler;
        Volatile.Write(ref handlers, next);
    }
}

public void Publish(T value)
{
    foreach (var handler in Volatile.Read(ref handlers))
    {
        handler(value);
    }
}
```

**ユースケース:** メッセージバス、オブザーバ、変更通知など購読者が複数のイベント機構。

**リポジトリ内実装:** [HandlerList.cs](src/PerformancePatterns/Dsp/HandlerList.cs) / [テスト](tests/PerformancePatterns.Tests/Dsp/HandlerListTest.cs) / [ベンチマーク](benchmarks/PerformancePatterns.Benchmarks/Dsp/HandlerListBenchmark.cs) / [測定結果](benchmarks/results/DSP-03-HandlerList.md)

**実測結果(net10 / x86-64-v4、購読者数別):**

| 購読者数 | マルチキャスト | 不変配列 | 比率 |
|---:|---:|---:|---|
| 1 | 0.12 ns | 0.68 ns | 5.72(❌ 配列が遅い) |
| 2 | 3.49 ns | 1.11 ns | **0.32** |
| 4 | 6.05 ns | 1.85 ns | 0.31 |
| 8 | 11.00 ns | 3.47 ns | 0.32 |

マルチキャストは購読者数にほぼ比例して悪化する一方、配列は緩やかに増えるだけ。**損益分岐は購読者 2 個**。

**注意:** 購読 1 個が支配的な用途では単一デリゲートのままが最速(単一デリゲートはループなしの直接 Invoke になる)。購読解除の頻度が高い場合は配列再構築コストも考慮する。

---

### 🚦 DSP-04: static ラムダの徹底(TState 引き回し)

**目的:** **ラムダ・ローカル関数は `static` を既定にする。** `static` 修飾でコンパイラにキャプチャを禁止させ、状態が必要な場合はキャプチャではなく `TState` 引数で明示的に渡す。

**効果:**

- 外部変数をキャプチャするラムダは表示クラス + デリゲートを確保しうる(ループ内・ホットパスでは呼び出しごと)。`static` なら混入がコンパイルエラーで防がれ、デリゲートはコンパイラがキャッシュして割り当てゼロになる
- 「外部状態に依存しない」ことがシグネチャで明示され、レビュー・生成コードの検証も容易になる
- BCL 自体が state 付き API を用意している(`ConcurrentDictionary.GetOrAdd(key, factory, state)`、`string.Create(length, state, action)`、`CancellationToken.Register(callback, state)`、`Task.ContinueWith(action, state)`)

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// ❌ キャプチャするラムダ: クロージャを確保、意図しない依存も混入しうる
var found = list.Find(x => x.Id == targetId);

// ✅ まず static を付ける。状態が必要なら TState で渡す
var found = list.Find(targetId, static (x, id) => x.Id == id);

// 複数値はタプルを state に載せる
var item = cache.GetOrAdd(key, static (k, s) => s.factory.Create(k, s.options), (factory, options));

// ✅ API 側: コールバックを受ける公開 API には TState オーバーロードを併設する
public T? Find<TState>(TState state, Func<T, TState, bool> predicate) { ... }
```

**ユースケース:** LINQ 風ユーティリティ、コレクション検索、辞書の GetOrAdd、継続・コールバック登録、Result/Option 型の Map/Bind。

**実測結果(net10 / x86-64-v4、反復ごとに変わるローカルを条件に使う検索):** キャプチャするラムダ 7.09 ns + **88 B/回**(クロージャ + デリゲート)に対し、static ラムダ + TState は **2.96 ns / 0 B(0.42 倍)**(コンパイラがデリゲートをキャッシュ)。→ [測定結果](benchmarks/results/DSP-04-StaticLambda.md)

**設計指針:** 手順として「①ラムダにはまず `static` を付ける → ②コンパイルエラーになったらその状態が本当に必要か見直す → ③必要なら `TState` で渡す」を規約化する。STK-04(static ローカルメソッド iterator)と同じ原則の適用範囲拡大であり、コールバックを受ける公開 API 側は TState 版を常に用意して呼び出し側がこの規約を守れるようにする。

---

### 🚦 DSP-05: デリゲート・パイプラインの事前確定

**目的:** 実行時に毎回行っている「合成・分岐解決・デリゲート生成」を初期化時に 1 回へ寄せる。

**効果:**

- ミドルウェア・フィルタのチェーンを起動時に組み立てておけば、リクエストごとの合成コストとデリゲート確保が消える
- 要素がゼロなら委譲そのものを作らず本体を直接呼ぶ「完全バイパス」ができる
- 毎レンダー・毎呼び出しで新しいラムダを渡す実装は、参照が毎回変わるため下流のキャッシュ・差分検出も無効化する(UI フレームワークで顕著)

**AOT:** ✅ 問題なし

**実装例:**

```csharp
public sealed class Pipeline
{
    private readonly Func<Context, ValueTask>? composed;

    public Pipeline(IReadOnlyList<IFilter> filters, Func<Context, ValueTask> terminal)
    {
        // ✅ 起動時に 1 回だけ合成。空ならデリゲートを作らない
        composed = filters.Count == 0 ? null : Compose(filters, terminal);
        this.terminal = terminal;
    }

    public ValueTask InvokeAsync(Context context)
        => composed is null ? terminal(context) : composed(context);
}

// ✅ コールバック・描画断片はコンストラクタで確定させ、以後は同じ参照を渡す
private readonly Action<int> onChanged;
public Widget() => onChanged = HandleChanged;
```

**ユースケース:** ミドルウェア/フィルタチェーン、UI の描画断片(RenderFragment)、コマンドの CanExecute、条件分岐の初期化時解決。

**関連する小技:** 再購読・再設定が毎回呼ばれる API(`OnParametersSet` 等)では、`ReferenceEquals` で前回対象と比較して**変化がなければ処理ごとスキップ**する。

**実測結果(net10 / x86-64-v4、ミドルウェア 3 段):** 毎回合成 19.7 ns + 264 B(クロージャ 3 個 + デリゲート生成)に対し、**事前合成 1.27 ns / 0 B(0.064 倍 = 約 16 倍)**。素の終端呼び出しはほぼ 0 ns なので、事前合成後のチェーン通過コストは 3 段で約 1.3 ns に収まる。→ [測定結果](benchmarks/results/DSP-05-PipelineCompose.md)

**注意:** 合成の複雑さと呼び出し頻度に効果が比例する。

---

## 🏷️ TYP: 型システム活用

### 🏷️ TYP-01: 静的型スロット(TypeMap / TypeSlot)

**目的:** `Type` をキーにした辞書を、ハッシュ計算なしの配列インデクスアクセスに置き換える。

**効果:**

- `TypeSlot<T>.Index` は JIT が定数として扱うため、実質的に「配列への直接添字アクセス」になる
- ハッシュ計算・衝突解決・ロックが不要
- 実測例: `Dictionary<Type, T>` ベースのスレッドセーフ実装の約 6 倍高速(シングルスレッド読み取り)

**AOT:** ⚠️ 条件付き

- ジェネリック API(`TryGetValue<T>()`)経由のアクセスは AOT で問題なく動作する(ジェネリック static フィールドは AOT 互換)
- 実行時の `Type` オブジェクトからスロットを確保するために `typeof(TypeSlot<>).MakeGenericType(type)` を使う実装は **IL3050(AOT 非互換)**。実行時 `Type` パスは `Dictionary<Type, int>` フォールバックで実装すること

**実装例:**

```csharp
internal static class TypeSlot
{
    private static int nextIndex = -1;

    public static int Next() => Interlocked.Increment(ref nextIndex);
}

internal static class TypeSlot<T>
{
    // 型ごとに一度だけ採番される。JIT はこれを定数として扱う
    public static readonly int Index = TypeSlot.Next();
}

// 使用側
// JIT 解決パス(型引数が既知の場合)
map.TryGetValue<MyService>(out var svc);

// ランタイム解決パス(型が動的な場合、Dictionary フォールバック)
map.TryGetValue(typeof(MyService), out var svc);
```

**ユースケース:** DI コンテナ、型ベースのハンドラ/ファクトリ登録、コンポーネントキャッシュ。

**リポジトリ内実装:** [TypeMap.cs](src/PerformancePatterns/Typ/TypeMap.cs) / [TypeSlot.cs](src/PerformancePatterns/Typ/TypeSlot.cs) / [テスト](tests/PerformancePatterns.Tests/Typ/TypeMapTest.cs) / [ベンチマーク](benchmarks/PerformancePatterns.Benchmarks/Typ/TypeMapBenchmark.cs) / [測定結果](benchmarks/results/TYP-01-TypeMap.md)

**実測結果(net10 / x86-64-v4、8 型の登録に対する解決):**

| 経路 | 時間 | 比率 | コードサイズ |
|---|---:|---|---:|
| `Dictionary<Type, T>`(基準) | 2.47 ns | 1.00 | 921 B |
| `FrozenDictionary` | 3.07 ns | 1.25(❌ 遅い) | 45 B |
| **TypeMap ジェネリック経路** | **0.23 ns** | **0.09(約 11 倍)** | 34 B |
| TypeMap 実行時 Type 経路 | 10.4 ns | 4.22(❌ 遅い) | 3,486 B |

**価値はジェネリック経路にのみある**(スロット番号が JIT 定数になり、実質「配列への添字アクセス」になる)。実行時 Type 経路は辞書引き + 配列アクセスの二段になるため素の Dictionary より遅く、型が静的に分かる呼び出しを主経路に設計できる場合にのみ採用する。

**注意:** スロット配列の拡張は lock + 配列差し替え(copy-on-write)で行い、読み取りパスをロックフリーに保つ。上記実装は実行時 Type → スロットの対応を `Dictionary<Type, int>` で持つため、`MakeGenericType` を使わず **AOT 安全**。

---

### 🏷️ TYP-02: BitwiseComparer\<T\>(生バイト比較)

**目的:** `unmanaged` 値型の等値・順序比較を生バイト列で行い、`Equals` オーバーライドを無視する。

**効果:**

- カスタム `Equals` を持つ値型を意図通りに辞書/セットのキーにできる
- SIMD 最適化された `SequenceEqual` / `SequenceCompareTo` で高速比較

**AOT:** ✅ 問題なし

**実装例:**

```csharp
var dict = new Dictionary<MyStruct, string>(BitwiseComparer<MyStruct>.Instance);
```

```csharp
public sealed class BitwiseComparer<T> : IEqualityComparer<T>
    where T : unmanaged
{
    public static BitwiseComparer<T> Instance { get; } = new();

    public bool Equals(T x, T y) =>
        MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref x, 1))
            .SequenceEqual(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref y, 1)));

    // GetHashCode はバイト列からのハッシュ計算で実装
}
```

**ユースケース:** ビットパターンで同一性を判定すべきケース(色、フラグ、ベクター等)。

**リポジトリ内実装:** [BitwiseComparer.cs](src/PerformancePatterns/Typ/BitwiseComparer.cs)(`IEqualityComparer<T>` + `IComparer<T>`、ハッシュは `HashCode.AddBytes`) / [テスト](tests/PerformancePatterns.Tests/Typ/BitwiseComparerTest.cs) / [ベンチマーク](benchmarks/PerformancePatterns.Benchmarks/Typ/BitwiseComparerBenchmark.cs) / [測定結果](benchmarks/results/TYP-02-BitwiseComparer.md)

**実測結果(net10 / x86-64-v4、16 バイト構造体キーの辞書ルックアップ 1 回あたり):**

| 比較子 | 時間 | 比率 | 割り当て |
|---|---:|---|---:|
| 既定比較子 + IEquatable なし struct(基準) | 15.8 ns | 1.00 | **96 B(❌ ボックス化)** |
| **BitwiseComparer + 同じ struct** | 8.4 ns | **0.54** | 0 B |
| 既定比較子 + IEquatable 実装 struct | 3.7 ns | 0.23 | 0 B |

**IEquatable を実装していない struct を既定比較子で辞書キーにすると、ルックアップごとにボックス化が発生する**。BitwiseComparer は Equals を書かずにこれを 0.54 倍 + 割り当てゼロへ改善する。ただし手書きの `IEquatable` 実装(0.23)が最速なので、**型を自分で所有しているなら IEquatable を実装するのが第一選択**。本比較子は外部型・カスタム Equals の迂回・比較子を型パラメータで差し替える生成コード向け。

**注意:** パディングを含む構造体は未初期化パディングバイトにより「論理的に等しいのに不一致」となる可能性がある。パディングのないレイアウト(または `Pack = 1`)の型に限定して使用する。

---

### 🏷️ TYP-03: UnsafeAccessor(非公開メンバーへの直接アクセス)

**目的:** private / internal なフィールド・メソッド・コンストラクタへ、リフレクションを使わず直接アクセスする(.NET 8+)。

**効果:**

- コンパイル時にシグネチャが解決され、直接呼び出し・直接フィールドアクセスと同等の速度になる(`MethodInfo.Invoke` 経由と比べ桁違いに高速)
- リフレクション呼び出しに伴うボックス化・引数配列のアロケーションが消える
- BCL 内部の最適化済みメソッド(公開 API では分岐が余分に入るもの等)を直接呼び出す用途にも使える

**AOT:** ✅ 問題なし。コンパイル時バインドのため Native AOT で動作し、トリミングでも参照先メンバーが保持される(private リフレクションの AOT 互換な代替になる)

**実装例:**

```csharp
// BCL 内部の static メソッドを直接呼び出す
// (static メソッドは第 1 引数の型で対象型を指定し、値は null を渡す)
[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "GetHashCodeOrdinalIgnoreCase")]
private static extern int GetHashCodeOrdinalIgnoreCase(string? self, ReadOnlySpan<char> value);

var hash = GetHashCodeOrdinalIgnoreCase(null, span);
```

```csharp
// 非公開フィールドへの ref アクセス
[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_message")]
private static extern ref string? GetMessageField(Exception exception);
```

**ユースケース:** BCL・サードパーティ内部 API の高速呼び出し、シリアライザ等での非公開フィールドアクセス、テスト用アクセサ。

**注意:**

- 対象メンバーは名前文字列で指定するため、参照先ライブラリの内部実装変更で実行時エラー(`MissingFieldException` / `MissingMethodException`)になる。内部 API は互換性契約の対象外である前提で、バージョン更新時にテストで検出できる体制を作る
- ジェネリック型・ジェネリックメソッド対応は .NET 9 以降(.NET 8 では非対応)。非公開「型」自体を扱う場合は .NET 10 の `UnsafeAccessorTypeAttribute` を使う
- 自分のコードベース内では通常の internal + `InternalsVisibleTo` を優先し、UnsafeAccessor は「変更できない外部コード」への手段と位置づける

**実測結果(net10 / x86-64-v4、非公開 int フィールドの読み出し):** UnsafeAccessor 0.192 ns = 公開プロパティ 0.192 ns(**コードサイズ 23 B で同一 = 直接フィールドロードへコンパイル**)。`FieldInfo.GetValue` は 4.77 ns + **24 B/回のボックス化**(24.9 倍)。→ [測定結果](benchmarks/results/TYP-03-UnsafeAccessor.md)

---

### 🏷️ TYP-04: ジェネリック static クラスによる型別キャッシュ

**目的:** 型ごとに一度だけ計算した成果物(コンバータ、デリゲート、メタデータ)を `static class Cache<T>` の static フィールドに保持し、辞書検索なしで取得する。

**効果(実測例):** `TypeDescriptor.GetConverter` を毎回呼ぶ実装 36.3ns → static キャッシュ 7.96ns(約 4.6 倍)。TYP-01(TypeSlot)はこのパターンの応用形。

**AOT:** ✅ パターン自体は問題なし(ジェネリック static フィールドは AOT 互換)。ただしキャッシュする内容の生成側がリフレクション由来(`TypeDescriptor` 等)の場合は、その API 自体のトリミング対応が別途必要([aot-compatibility.md](docs/aot-compatibility.md) AOTP-05)

**実装例:**

```csharp
private static class ConverterCache<T>
{
    public static readonly TypeConverter Converter = TypeDescriptor.GetConverter(typeof(T));
}

public static T? Convert<T>(string value)
    => (T?)ConverterCache<T>.Converter.ConvertFromInvariantString(value);
```

**ユースケース:** 型変換層、シリアライザのフォーマッタ解決、型メタデータの保持。

**リポジトリ内実装:** [TypeSlot.cs](src/PerformancePatterns/Typ/TypeSlot.cs)(`TypeSlot<T>.Index` — 本パターンの最小形。TYP-01 の土台)

**実測結果:** 同型の測定(TYP-06)で、ジェネリック static フィールド読みは **〜0 ns / コードサイズ 6 B**(計測分解能以下。`Dictionary<Type, T>` キャッシュは 2.7 ns)→ [TYP-06-StaticArtifact.md](benchmarks/results/TYP-06-StaticArtifact.md)

**注意:** static コンストラクタの初期化は型ごとに初回 1 回のみ。失敗しうる初期化を入れると `TypeInitializationException` が以後もキャッシュされるため、失敗時は「未対応」を表すフォールバック値を入れる設計にする。

---

### 🏷️ TYP-05: Unsafe.As による型チェック省略キャスト

**目的:** 型の対応関係をレジストリ設計で構造的に保証できる場合に、通常キャストの実行時型チェックを `Unsafe.As` で省略する。

**効果(実測例):**

- `(Action<object?>)obj` 3.43ns → `Unsafe.As<Action<object?>>(obj)` 1.59ns(約 2 倍)、コードサイズ 498B → 67B
- DI レジストリの型付き解決(`Resolve<T>`)でも約 1.7 倍 + ジェネリックインスタンス化ごとのキャストコード膨張を抑制

**実測結果(net10 / x86-64-v4、型が構造的に保証された object[] 1024 要素):**

| 方式 | 時間 | 比率 | コードサイズ |
|---|---:|---|---:|
| `(string)value`(castclass) | 335.5 ns | 1.00 | 274 B |
| `is string text` パターン | 324.7 ns | 0.97 | 57 B |
| **`Unsafe.As<string>(value)`** | **212.8 ns** | **0.63** | **33 B** |

castclass はキャストヘルパーと例外パスを伴うためコードサイズが 8 倍。**型不変条件をレジストリ設計で保証できる場合に限り** Unsafe.As を使う(誤った型は静かなメモリ破壊になる)。→ [測定結果](benchmarks/results/TYP-05-UnsafeAsCast.md)

**AOT:** ✅ 問題なし

**実装例:**

```csharp
private readonly Dictionary<Type, object> factories = new();

public void Register<T>(Func<T> factory) => factories[typeof(T)] = factory;

public T Resolve<T>()
{
    // Register<T> でしか登録できないため typeof(T) → Func<T> の対応は構造的に保証される
    var factory = factories[typeof(T)];
    return Unsafe.As<Func<T>>(factory)();
}
```

**ユースケース:** 型キーのレジストリ(DI・フォーマッタ表・ハンドラ表)の解決パス。

**注意:**

- 型対応の保証が崩れると `InvalidCastException` にならず黙って壊れる(未定義動作)。登録 API 側で型安全を担保し、`Unsafe.As` は private 境界に閉じ込める
- Debug ビルドでは通常キャスト + `Debug.Assert` で検証し、Release のみ `Unsafe.As` にする構成も有効

---

### 🏷️ TYP-06: 型別成果物の静的事前組み立て

**目的:** 型ごとに決まる文字列・メタデータ(SQL 断片、型名、書式)を、実行時に組み立て直さず**ジェネリック static の初期化で 1 回だけ**確定させる。

**効果:**

- 実行時は静的フィールドの読み出しのみになり、辞書引きも文字列連結も発生しない(TYP-04 の「成果物が文字列・SQL」版)
- 型初期化子は型ごとに一度だけ走るため、初期化コストは償却される
- 連結は 1 回の `String.Concat` に畳める。区切り文字は「常に後置して最後に `Length -= n`」とすると先頭判定の分岐が消える

**AOT:** ✅ 問題なし(ジェネリック static は AOT 互換。ただし内容の生成にリフレクションを使う場合はその API のトリミング対応が別途必要)

**実装例:**

```csharp
internal static class SqlInsert<T>
{
    // 型初期化子で 1 回だけ組み立て、以後は静的フィールドの読み出しのみ
    public static readonly string Sql = Build();

    private static string Build()
    {
        var builder = new StringBuilder();
        builder.Append("INSERT INTO ").Append(TableName<T>.Value).Append(" (");
        foreach (var column in Columns<T>.All)
        {
            builder.Append(column.Name).Append(", ");   // 常に後置
        }

        builder.Length -= 2;                             // 最後にまとめて削る
        return builder.Append(") VALUES (...)").ToString();
    }
}

// 使用側は辞書引きなしの静的読み出し
var sql = SqlInsert<Order>.Sql;
```

**ユースケース:** O/R マッパーの SQL 生成、型名を含むログ・診断文字列、シリアライザのスキーマ断片。

**実測結果(net10 / x86-64-v4、SQL 断片の取得):**

| 方式 | 時間 | 比率 | 割り当て | コードサイズ |
|---|---:|---|---:|---:|
| 毎回組み立て(基準) | 57.0 ns | 1.00 | 760 B | 5,220 B |
| `Dictionary<Type, string>` キャッシュ | 2.7 ns | 0.048 | 0 B | 936 B |
| **ジェネリック static フィールド** | **〜0 ns** | **0.000** | **0 B** | **6 B** |

静的フィールド読み出しは実質タダ — 計測分解能以下(TYP-01 のジェネリック経路と同じ構図)。型が静的に分からない呼び出しにのみ辞書を使う。→ [測定結果](benchmarks/results/TYP-06-StaticArtifact.md)

**注意:** 型初期化子で例外が起きると `TypeInitializationException` が以後キャッシュされ続ける。失敗しうる生成は「未対応」を表すフォールバック値を入れる設計にする(TYP-04 と同じ注意)。

---

## 🔢 BIT: ビット演算・ブランチレス最適化

### 🔢 BIT-01: ドメイン制約を活かした軽量ハッシュ生成

**目的:** 汎用ハッシュ(`string.GetHashCode` 等)が持つ「全文字反映・高い分散品質」をドメイン制約に基づいて捨て、O(1) の専用ハッシュに置き換える。

**効果:**

- 文字列長に依存しない定数時間でハッシュ値を生成できる
- 実測例: `string.GetHashCode(ReadOnlySpan<char>)` 比で約 8.5 倍、OrdinalIgnoreCase 版比で約 4 倍
- 大文字小文字を無視する場合も、サンプリングした文字だけ正規化すればよい(全文字の ToUpper 走査が不要)

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// 長さ + 先頭・中央・末尾の 3 文字だけをシフト/XOR で合成する
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static int GetHashCode(ReadOnlySpan<char> value)
{
    var length = value.Length;
    if (length is 0)
    {
        return 0;
    }

    ref var head = ref MemoryMarshal.GetReference(value);
    var first = Unsafe.Add(ref head, 0);
    var middle = Unsafe.Add(ref head, length >> 1);
    var last = Unsafe.Add(ref head, length - 1);
    return (length << 16) ^ (first << 8) ^ (middle << 4) ^ last;
}
```

```csharp
// 大文字小文字を無視する場合: サンプリングした 3 文字のみ正規化
return (length << 16)
    ^ (char.ToUpperInvariant(first) << 8)
    ^ (char.ToUpperInvariant(middle) << 4)
    ^ char.ToUpperInvariant(last);
```

**ユースケース:** 列挙型名の逆引き、キーワードテーブル、プロトコルヘッダ名など「短い識別子 × 既知の少数集合」をキーにした検索。

**設計指針:** 汎用実装が保証する品質(全文字反映・分散・衝突耐性)がそのドメインで本当に必要かを問い、不要なら捨てる。「キーは短い・少数・既知」という制約を性能に変換するのがこのパターンの本質。

**注意:**

- 衝突は当然起こりうる(例: `AxxxBxxxC` と `AyyyByyyC` は同値)。ハッシュ一致後の完全一致比較を必ず併用し、実際のキー集合で衝突率を確認する
- シード・ランダム化がないため hash flooding(意図的な衝突キーの大量投入)に無防備。外部入力をキーとして受け付ける汎用ハッシュテーブルには使用せず、閉じた既知集合専用とする
- 実装の `CalculateHash` は 3 文字の取得に手動 ref(`GetReference` + `Unsafe.Add`)を使う。**索引形は `value[length >> 1]` の境界チェックを 1 本除去できず**(Tier1 で RNGCHKFAIL 経路が残存、128 B vs 115 B・56 vs 49 命令)、時間差は分解能以下 — R-02 の例外(構成的に範囲保証されたサンプリングアクセス)としてこの形を維持している

**リポジトリ内実装:** [SampledNameTable.cs](src/PerformancePatterns/Col/SampledNameTable.cs) の `CalculateHash`(実測は [COL-04](benchmarks/results/COL-04-SampledNameTable.md))

**適用範囲の実測知見:**

- C# コンパイラの文字列 switch(少数なら長さ+文字判定、多数なら全文ハッシュ+ジャンプテーブルへ lowering)との比較では一律の勝者はない。少数(〜4 件)はコンパイラ生成が速く、中規模(〜12 件)や共通接頭辞で衝突しやすいキー集合ではサンプリングハッシュ switch が約 2 倍速く、大規模(32 件〜)では再びコンパイラ生成が優位になる
- 大文字小文字を無視した enum 名パースでは `Enum.TryParse`(ignoreCase)比で 0.11〜0.24 倍と圧倒的(素の文字列 switch は Ordinal になるため ignoreCase 用途に使えず、このパターンの独壇場)。数件以下なら `Equals(OrdinalIgnoreCase)` の if 連鎖(0.17 倍)で足りる
- 固定の先頭/中央/末尾サンプリングが衝突するキー集合では、コード生成(Source Generator)時に衝突しないサンプリング位置を探索して定数として埋め込む

---

### 🔢 BIT-02: 2 の累乗サイズ + マスクによる剰余置換

**目的:** ハッシュ表などのインデックス計算で `% length` の整数除算を避け、サイズを 2 の累乗に揃えて `& (length - 1)` のマスクに置き換える。

**効果:** 整数除算(数十サイクル)がビット AND(1 サイクル)になる。符号付き `%` は負数対応の補正命令も入るためさらに重い。

**AOT:** ✅ 問題なし

**実装例:**

```csharp
var size = (int)BitOperations.RoundUpToPowerOf2((uint)requested);
var mask = size - 1;
// ...
var index = hash & mask;
```

**ユースケース:** 自前ハッシュ表、リングバッファ、プールのバケット計算。

**リポジトリ内実装:** [SampledNameTable.cs](src/PerformancePatterns/Col/SampledNameTable.cs)(バケット数を 2 の累乗に丸めて `hash & mask` で添字化)

**実測結果(net10 / x86-64-v4、1024 回のバケット添字計算):**

| 方式 | 時間 | 比率 |
|---|---:|---|
| 実行時サイズの `%`(除算命令) | 1,203.5 ns | 1.00 |
| **2 の累乗マスク `&`** | 215.3 ns | **0.18** |
| 定数サイズの `%` | 213.3 ns | 0.18 |

**手動マスク化が必要なのは「サイズが実行時に決まる」場合のみ**。定数の 2 の累乗に対する `%` は JIT が既に AND 形へ落とすため、そのまま書いてよい(マスク版と定数 `%` はコード 51 B・時間とも同一)。→ [測定結果](benchmarks/results/BIT-02-PowerOfTwoMask.md)

**注意:**

- 符号付き int の `/ 2` や `% 2` は JIT が単純シフトに落とせない(負数補正が入る)。非負が保証できるなら uint 化または符号なし右シフト `>>>`(C# 11)を使う
- 境界チェック除去目的の無条件な uint キャスト小細工は最近のランタイムでは効果が消えていることが実測されている(範囲チェックの手書き変換も JIT が自動で融合する — R-18)

---

### 🔢 BIT-03: BitOperations によるビット走査・計数

**目的:** ビットマップの走査・ビット数計測を素朴なループからハードウェア命令(`TrailingZeroCount` / `PopCount` / `Log2` 等)へ置き換える。

**効果(実測、net10 / x86-64-v4、7 ビット立った疎な ulong × 64 個):**

- 立ちビット走査: 全 64 ビットループ 1,056ns → TZCNT 方式 **141ns(0.13、7.5 倍)**
- ビット数計測: 手動ループ 854ns → `PopCount` **12.8ns(0.01、67 倍)**

**AOT:** ✅ 問題なし(対応 CPU ではハードウェア命令、未対応環境はソフトウェアフォールバック)

**実装例:**

```csharp
// 立っているビットだけを辿る: 最下位の立ちビット位置を取得し、mask &= mask - 1 で消す
while (mask != 0UL)
{
    var bit = BitOperations.TrailingZeroCount(mask);
    ProcessSlot(bit);
    mask &= mask - 1;
}
```

**ユースケース:** ビットマップアロケータ・プールの空きスロット検索、sparse set、フラグ集計、`RoundUpToPowerOf2`(BIT-02)や `Log2` によるサイズ計算。

---

### 🔢 BIT-04: XxHash3 による汎用ハッシュ

**目的:** 非暗号ハッシュ(キャッシュキー、チェックサム、重複検出)を、自前 FNV-1a や `string.GetHashCode` ではなく最適化済み実装に任せる(`System.IO.Hashing`)。

**効果:**

- XxHash3 は長い入力でスループットが高く、`HashToUInt64` / `Hash` の静的 API で使える
- `char` 列は `MemoryMarshal.Cast<char, byte>` で byte として再解釈でき、この変換は**実測でゼロコスト**(`fixed` ポインタと差がない)
- `string.GetHashCode` はプロセスごとにランダム化されるため、**永続化・プロセス間で安定した値が必要な場合は使えない**。XxHash3 は安定

**AOT:** ✅ 問題なし(NuGet: System.IO.Hashing)

**実装例:**

```csharp
using System.IO.Hashing;

// char 列を byte として再解釈して計算(コピーなし)
var hash = XxHash3.HashToUInt64(MemoryMarshal.AsBytes(value.AsSpan()));
```

**ユースケース:** 分散キャッシュキー、ファイル・バッファのチェックサム、シャーディング。

**使い分け:**

- 少数の既知キー集合 → BIT-01(サンプリングハッシュ)。全文を読まないぶん更に速い
- 短い ASCII トークン判定 → TXT-04(バイト列の直接比較)
- 汎用・長い入力・安定値が必要 → 本パターン

**実測結果(net10 / x86-64-v4、string.GetHashCode 比):**

| 実装 | 8 文字 | 64 文字 | 512 文字 |
|---|---|---|---|
| `string.GetHashCode`(基準) | 1.00 | 1.00 | 1.00 |
| **XxHash3(Cast 経由)** | **0.26** | **0.21** | **0.09** |
| XxHash3(fixed 経由) | 0.25 | 0.21 | 0.07 |
| FNV-1a 手書きループ | 0.45 | **1.05(❌)** | **1.53(❌)** |
| サンプリングハッシュ(BIT-01) | 0.06 | 0.008 | 0.001 |

XxHash3 は 8 文字時点で既に速く、長いほど差が開く。`MemoryMarshal.AsBytes` と `fixed` は同等(pinning が不要なぶん Cast を推奨)— ゼロコスト再解釈の確認としては十分。手書きのハッシュループ(FNV-1a)は 64 文字以降でベクトル化された BCL に負けるため、自作しない。→ [測定結果](benchmarks/results/BIT-04-XxHash3.md)

**注意:** 非暗号ハッシュのため、改ざん検知や署名には使えない。

---

## 🧮 VEC: SIMD・ベクトル化

### 🧮 VEC-01: 明示的 SIMD(Vector\<T\> / Vector256)

**目的:** 集計・変換・検索のデータ並列処理をハードウェア SIMD 命令で一括実行する。

**効果(実測、net10 / x86-64-v4(AVX-512)、int[4096] の合計):**

| 実装 | 比率 |
|---|---|
| スカラーループ | 1.00(826ns) |
| `Enumerable.Sum`(BCL、ベクトル化済み) | 0.31 |
| `Vector256` 直接 | 0.22 |
| **`Vector<T>`(幅非依存)** | **0.14(7.0 倍)** |

明示的 SIMD がスカラーループに大差で勝つこと自体は SIMD 対応 CPU なら共通で、この方針はハードウェアに依存しない。**どの形が最速かは CPU 幅に依存する:** `Vector<T>` はハードウェア幅に追従する(AVX-512 では int 16 レーン)一方、`Vector256` は 8 レーン固定のため、ここでは幅非依存形が勝つ。AVX2 までの CPU では両者とも 8 レーンで同等になりうる。**既定は幅非依存の `Vector<T>`** とし、幅の決め打ちはアルゴリズムが特定レーン構成を要求する場合に限る。

**AOT:** ✅ 問題なし(AOT でもターゲット ISA の SIMD 命令が使われる。`IsHardwareAccelerated` ガード + スカラーフォールバックを用意する)

**実装例:**

```csharp
ref var start = ref MemoryMarshal.GetArrayDataReference(values);
var acc = Vector256<int>.Zero;
var i = 0;
for (; i <= values.Length - Vector256<int>.Count; i += Vector256<int>.Count)
{
    acc += Vector256.LoadUnsafe(ref start, (nuint)i);
}

var total = Vector256.Sum(acc);
for (; i < values.Length; i++)
{
    total += Unsafe.Add(ref start, i); // 端数はスカラーで処理
}
```

**設計指針(重要):** まず BCL のベクトル化済み API(`Enumerable.Sum`、`IndexOf`/`SequenceEqual`、`SearchValues`、`Ascii`、`TensorPrimitives`)を探す — スカラー比 4 倍超が書かずに手に入る。自前 SIMD は「BCL に該当 API がない処理」に限定し、`Vector<T>`(可搬)→ `Vector128/256`(制御重視)の順で検討する。

**ユースケース:** チェックサム・集計、独自エンコード/デコード、数値変換の一括適用。

**注意:** 端数処理・非対応 CPU フォールバックのテストを必ず用意する(Verify で全経路をスカラー実装と突き合わせる)。

---

## 📜 SEQ: 逐次読み書き・シーケンス処理

### 📜 SEQ-01: SpanTokenizer\<T\>

**目的:** 任意の `IEquatable<T>` 型のスパンを区切り要素で分割する汎用ゼロアロケーショントークナイザ。

**効果:**

- `string.Split` と違い配列を生成しない
- char 以外の型(int, byte など)でも同じコードが使える
- `foreach` のダックタイピングで自然な構文

**AOT:** ✅ 問題なし

**実装例:**

```csharp
foreach (var token in new SpanTokenizer<char>(line.AsSpan(), ','))
{
    // ReadOnlySpan<char> — ゼロアロケーション
}
```

**ユースケース:** CSV 解析、プロトコルヘッダ分割、コマンド引数パース。

**関連:** STK-01(ref struct) + STK-03(struct iterator) + JIT-02(IEquatable 制約)の複合適用例。.NET 9+ の `MemoryExtensions.Split` も同種の機能を提供するため、要件に応じて使い分ける。

**リポジトリ内実装:** [SpanTokenizer.cs](src/PerformancePatterns/Seq/SpanTokenizer.cs) / [テスト](tests/PerformancePatterns.Tests/Seq/SpanTokenizerTest.cs) / [ベンチマーク](benchmarks/PerformancePatterns.Benchmarks/Seq/SpanTokenizerBenchmark.cs) / [測定結果](benchmarks/results/SEQ-01-SpanTokenizer.md)

**実測結果(net10 / x86-64-v4):** `string.Split` 比で 4 トークン時 0.47 倍だが、**64 トークン時は 1.15 倍(遅い)** — 長い入力では string.Split のベクトル化走査が勝る。アロケーションは全ケースで 216B〜3,096B → **0B**。.NET 9+ の `MemoryExtensions.Split` と比べると 4〜13% 高速でコードサイズも小さい(707B vs 910B)。採用理由はアロケーション排除と短い入力であり、無条件の速度優位ではない。

---

### 📜 SEQ-02: Stream 構造体 I/O

**目的:** unmanaged 構造体を `Stream` と直接読み書きし、中間バッファ・シリアライザを排除する。

**効果:**

- 構造体のメモリレイアウトをそのままバイト列として扱うため変換コストゼロ
- ヘッダ・固定長レコードの読み書きが 1 回の I/O 呼び出しで完結

**AOT:** ✅ 問題なし

**実装例:**

```csharp
public static T Read<T>(this Stream stream) where T : unmanaged
{
    Unsafe.SkipInit(out T value);
    var span = MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref value), Unsafe.SizeOf<T>());
    stream.ReadExactly(span);
    return value;
}

public static void Write<T>(this Stream stream, in T value) where T : unmanaged
{
    var span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in value)), Unsafe.SizeOf<T>());
    stream.Write(span);
}
```

**ユースケース:** バイナリファイルフォーマット、固定長レコード I/O、独自プロトコル。

**実測結果(net10 / x86-64-v4、16 バイト × 1024 レコード):**

| 方式 | 時間 | 比率 |
|---|---:|---|
| 書き込み: `BinaryWriter` フィールド単位 | 10,199 ns | 1.00 |
| **書き込み: `MemoryMarshal.AsBytes` 一括** | **154.0 ns** | **0.015(約 66 倍)** |
| 読み取り: `BinaryReader` フィールド単位 | 4,609 ns | 1.00 |
| **読み取り: `ReadExactly` + 一括再解釈** | **93.8 ns** | **0.020(約 49 倍)** |

**本カタログ中で最大の改善幅**。フィールド単位 I/O は呼び出しごとにバッファ境界チェックと書式処理を通るのに対し、一括再解釈は 1 回の memcpy になる。→ [測定結果](benchmarks/results/SEQ-02-StructStreamIo.md)

**注意:** メモリレイアウトがそのまま外部形式になるため、`[StructLayout(LayoutKind.Sequential, Pack = 1)]` 等でレイアウトを固定し、エンディアン・パディングを設計として明示すること。異環境互換が必要な場合は `BinaryPrimitives` による明示変換を使う。

---

### 📜 SEQ-03: 遅延評価シーケンス処理(Batch / Segment / Traverse)

**目的:** シーケンス全体をマテリアライズせず、チャンク分割・階層走査を遅延評価かつ低アロケーションで行う。

**効果:**

- 入力全体の配列化・リスト化を回避し、ワーキングセットを一定に保つ
- STK-04(static ローカルメソッド iterator)との併用でクロージャアロケーションもゼロ

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// 1000 件ずつまとめて処理(全体を List 化しない)
foreach (var chunk in source.Batch(1000))
{
    BulkInsert(chunk);
}

// 木構造の走査(再帰スタック・中間コレクションなし)
foreach (var node in root.TraverseDepthFirst(static x => x.Children))
{
    Visit(node);
}
```

**ユースケース:** バルク処理、ページング、木構造・グラフの走査。

**リポジトリ内実装:** [BatchExtensions.cs](src/PerformancePatterns/Seq/BatchExtensions.cs)(Span 版 = ref struct enumerator でスライスのみ、配列版 = ArraySegment 返し) / [テスト](tests/PerformancePatterns.Tests/Seq/BatchTest.cs) / [ベンチマーク](benchmarks/PerformancePatterns.Benchmarks/Seq/BatchBenchmark.cs) / [測定結果](benchmarks/results/SEQ-03-Batch.md)

**実測結果(net10 / x86-64-v4、1024 要素を 100 件ずつ):**

| 方式 | 時間 | 比率 | 割り当て | コードサイズ |
|---|---:|---|---:|---:|
| `Enumerable.Chunk`(基準) | 359 ns | 1.00 | 4,424 B | 1,769 B |
| **配列 Batch(ArraySegment)** | 266 ns | 0.74 | **0 B** | 141 B |
| **Span Batch(スライス)** | **227 ns** | **0.63** | **0 B** | 108 B |

`Chunk` はチャンクごとに新しい配列を確保してコピーする。Batch はビュー(スライス / ArraySegment)を返すだけなので、割り当てもコピーも発生しない。

**関連:** .NET 9+ の `Enumerable.Chunk` / `Index` 等の標準 API で足りる場合はそちらを優先し、割り当てが問題になる高頻度パスで本実装のようなビュー返しへ切り替える。

---

### 📜 SEQ-04: リングバッファ + 増分デリミタ探索

**目的:** ストリーミング受信(シリアル・ソケット)のレコード分割で、再スキャンとコピーを最小化する。

**効果:**

- 探索開始位置を保持して**前回スキャン済みの領域を再走査しない**(未検出時は `count - delimiter.Length + 1` まで進める)
- 折り返しのないケースでは連続領域をそのままコールバックに渡せる(**完全ゼロコピー**)。折り返し時のみ連結コピーが必要
- リングを 2 つの連続セグメントに分けて `IndexOf`(ベクトル化済み)を効かせ、境界をまたぐ候補位置だけ手動照合する

**AOT:** ✅ 問題なし

**実装例(骨格):**

```csharp
// 受信データを ArrayPool 借用のリングへ蓄積し、行が揃うたびに通知する
private int head;      // 有効データの先頭
private int count;     // 有効データ長
private int search;    // 次に走査を再開する相対位置

private bool TryReadLine(out ReadOnlySpan<byte> line)
{
    var index = IndexOfDelimiter(search);
    if (index < 0)
    {
        // 見つからなかった分は再走査しない
        search = Math.Max(0, count - Delimiter.Length + 1);
        line = default;
        return false;
    }

    line = SliceWithoutCopyIfContiguous(index);
    search = 0;
    return true;
}
```

**ユースケース:** シリアル通信の行分割、TCP のフレーミング、ログテーリング。

**関連:** より高機能な選択肢として ASY-07(System.IO.Pipelines)がある。自前リングは「依存を増やさず・固定サイズで・オーバーフロー時の破棄方針を自分で決めたい」場合に選ぶ。

**実測結果(net10 / x86-64-v4、2 KB 行 × 16 を 256 B チャンクで受信):** 毎回全域再走査 + 行ごと前方詰め 1.70 μs に対し、**増分探索 + 遅延コンパクション 1.13 μs(0.67 倍)**。どちらもゼロアロケーションで、差は「走査済みバイトを再走査しない」「データ移動を行ごとでなく必要時のみにする」ことから生じる(折り返し 2 セグメント処理は未測定、フラットバッファ形での測定)。→ [測定結果](benchmarks/results/SEQ-04-RingSplit.md)

**注意:** バッファ超過時の方針(古いデータを捨てる / 例外 / 拡張)を明示的に決める。

---

## 🗃️ COL: コレクション最適化

### 🗃️ COL-01: CollectionsMarshal による内部直接アクセス

**目的:** `List<T>` / `Dictionary<TKey, TValue>` の公開 API を迂回し、内部ストレージへの Span / ref を直接取得する。

**効果(実測例):**

- `CollectionsMarshal.AsSpan(list)` で List 反復が約 2 倍(自環境 net10 実測 0.52、for/foreach とも)。`List<T>` の素の foreach / for は同速で最も遅い
- `GetValueRefOrAddDefault` で辞書の read-modify-write(`map[key]++` 相当)が 0.66 倍(自環境 net10。ハッシュ計算・探索が 2 回 → 1 回になる)
- `SetCount`(.NET 8+)+ Span 書き込みによる一括構築は Add ループ比 0.22〜0.26 倍(約 4 倍)。容量指定 Add(0.47〜0.60 倍)よりさらに 2 倍速く、成長再確保の排除でアロケーションも半減

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// List の Span 化反復
foreach (ref var item in CollectionsMarshal.AsSpan(list))
{
    item.Value++;
}

// 辞書のカウントアップ: 探索 1 回で読み書き
ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(map, key, out _);
count++;

// サイズ既知の一括構築: Add を使わず Count を確定してから Span で書く(.NET 8+)
var list = new List<int>();
CollectionsMarshal.SetCount(list, size);
var span = CollectionsMarshal.AsSpan(list);
for (var i = 0; i < span.Length; i++)
{
    span[i] = Compute(i);
}
```

**ユースケース:** 集計処理、キャッシュのヒットカウント、内部モデルの一括更新。

**リポジトリ内実装:** [ベンチマーク](benchmarks/PerformancePatterns.Benchmarks/Lab/CollectionsMarshalBenchmark.cs)([SetCount](benchmarks/PerformancePatterns.Benchmarks/Lab/ListSetCountBenchmark.cs)) / [測定結果](benchmarks/results/COL-01-CollectionsMarshal.md)

**注意:**

- `AsSpan` 保持中に List へ Add しない(内部配列の差し替えで Span が古い配列を指す)
- 旨味は「読み+書きの統合」にある。追加のみなら `TryAdd` と差はなく、`GetValueRefOrNullRef` + `Unsafe.IsNullRef` の存在チェックを挟むと効果が減る
- `SetCount` で拡張された領域は値型ではゼロ初期化が保証されない(旧値が見える)。読み出す前に必ず全域へ書き込む前提で使う

---

### 🗃️ COL-02: FrozenDictionary の条件付き採用

**目的:** 構築後に変化しない辞書を `FrozenDictionary` にして検索を高速化する。

**効果と適用条件(実測例):**

- 検索は `Dictionary` 比 2〜4 倍高速(1024 件)。ただし**構築は 15〜20 倍遅く**割り当ても大きい — 起動時に一度だけ構築して読み続ける用途限定
- キー集合によっては検索も逆転する(実測例: enum 名 64 件で Dictionary より 1.15〜1.31 倍遅い)。採用前に実データで計測する

**実測結果(net10 / x86-64-v4、string キー、非インターンのプローブ):**

| 観点 | 16 件 | 256 件 |
|---|---|---|
| 構築(Frozen / Dictionary) | **10.6 倍**(847 vs 80 ns) | **8.2 倍**(10,510 vs 1,282 ns) |
| 構築の割り当て | 4.25 倍 | 4.25 倍 |
| 検索(Frozen / Dictionary) | 1.00(➖誤差) | 0.98(➖誤差) |

**この条件(string キー・16〜256 件)では検索側に測定可能な利得がなく、8〜11 倍の構築コストが償却されない。** 採用は「実データで検索勝ちを実測できた場合」に限る。既知キー集合の名前解決なら COL-04(サンプリングハッシュ表、Dictionary 比 0.60〜0.62 倍)の方が確実。不採用側の一般記録は R-08。→ [測定結果](benchmarks/results/COL-02-FrozenCondition.md)
- `Type` キーの辞書では専用実装(TYP-01 系の型スロット、またはオープンアドレスの型ハッシュマップ)が FrozenDictionary の約 3 倍速い
- `ReadOnlyDictionary` ラッパーはラップ分だけ確実に遅くなる(不変性の表明には `FrozenDictionary` か `IReadOnlyDictionary` 公開を使う)

**AOT:** ✅ 問題なし

**ユースケース:** 設定テーブル、キーワード辞書、静的マッピング。

---

### 🗃️ COL-03: GetAlternateLookup による Span キー検索

**目的:** `Dictionary<string, TValue>` を `ReadOnlySpan<char>` のまま検索し、キーの `ToString()` アロケーションを排除する(.NET 9+)。

**効果(実測例):** `span.ToString()` してから引く実装は 2.4〜3.1 倍遅く必ずアロケートする。AlternateLookup は string キー検索とほぼ同速(1.05〜1.21 倍)でアロケーションゼロ。

**AOT:** ✅ 問題なし

**実装例:**

```csharp
private readonly Dictionary<string, int> map = new(StringComparer.Ordinal);
private readonly Dictionary<string, int>.AlternateLookup<ReadOnlySpan<char>> lookup;

public Resolver()
{
    lookup = map.GetAlternateLookup<ReadOnlySpan<char>>();
}

public bool TryResolve(ReadOnlySpan<char> name, out int value)
    => lookup.TryGetValue(name, out value);
```

**ユースケース:** パーサーのキーワード解決、プロトコルヘッダ解決、`ReadOnlySpan<char>` を受けるすべての名前引き API。

**実測結果(net10 / x86-64-v4、COL-04 の測定に含まれる):** Span キーの AlternateLookup は string キー直引きとほぼ同時間(4 / 16 / 32 件で 1.0 倍)。**span しか手元にない場面で `ToString()` の確保(+コピー)なしに引ける**ことが価値であり、既知キー集合ならサンプリングハッシュ表(COL-04)が AlternateLookup の 0.59〜0.75 倍でさらに速い。→ [測定結果](benchmarks/results/COL-04-SampledNameTable.md)

**注意:** comparer が `IAlternateEqualityComparer` を実装している必要がある(既定の string comparer / `StringComparer.Ordinal(IgnoreCase)` は対応済み)。`FrozenDictionary` / `HashSet` にも同 API がある。

---

### 🗃️ COL-04: 少数要素ルックアップの戦略選択

**目的:** 要素数・キーの性質・アクセスパターンに応じて、辞書 / 線形探索 / 分岐チェーン / ハッシュ switch を選び分ける。

**知見(実測例):**

- 〜8 件程度: `string.Equals` の if 連鎖が最速級(enum 名解決で `Enum.TryParse` 比 0.17 倍)。小規模では配列の線形探索も辞書より速い
- 十数件〜: サンプリングハッシュ switch(BIT-01)が安定して速い。Equals 連鎖は「宣言順どおりのアクセス」では速いが、逆順・部分アクセスでは 3〜5 倍劣化する — 平均ではなくアクセス形状への安定性で選ぶ
- FrozenDictionary はこの規模(〜32 件)では最速になりにくい(実測でどのカラム数でも最速にならなかった)

**AOT:** ✅ 問題なし

**ユースケース:** Source Generator が生成する名前→インデックス解決(DB カラム、プロパティ名、enum 名)、プロトコルのヘッダディスパッチ。

**設計指針:** 生成コードなら要素数が生成時に分かるため、件数に応じて Equals 連鎖(小)/ ハッシュ switch(中〜)を出し分けるのが理想。

**リポジトリ内実装:** [SampledNameTable.cs](src/PerformancePatterns/Col/SampledNameTable.cs)(BIT-01 のハッシュ + BIT-02 のマスク + バケット内 Ordinal 確定) / [テスト](tests/PerformancePatterns.Tests/Col/SampledNameTableTest.cs) / [ベンチマーク](benchmarks/PerformancePatterns.Benchmarks/Col/SampledNameTableBenchmark.cs) / [測定結果](benchmarks/results/COL-04-SampledNameTable.md)

**実測結果(net10 / x86-64-v4、名前解決を要素数別に):**

| 実装 | 4 件 | 16 件 | 32 件 |
|---|---|---|---|
| `Dictionary`(string キー、基準) | 1.00 | 1.00 | 1.00 |
| 線形探索 | 0.62 | 1.77(❌) | 3.23(❌) |
| サンプリングハッシュ表 | **0.60** | **0.62** | **0.75** |
| `Dictionary` AlternateLookup(Span キー基準) | 1.00 | 1.00 | 1.00 |
| `FrozenDictionary` AlternateLookup | 1.03 | 0.90 | 0.89 |
| **サンプリングハッシュ表(Span キー)** | **0.59** | **0.60** | **0.75** |

サンプリングハッシュ表は全サイズで安定して速く、Span キー比較では FrozenDictionary にも勝る。線形探索は 4 件では同等だがそれ以上で急速に劣化する。コードサイズも 692〜706 B と、Dictionary(約 1.1KB)・Frozen(約 2.1KB)より小さい。

---

### 🗃️ COL-05: IEnumerable 引数の具象型ディスパッチ

**目的:** `IEnumerable<T>` を受ける API の内部で実行時型を判定し、`List<T>`(および必要に応じて `T[]`)を Span パスへ逃がす(LINQ 内部の定石)。

**効果(実測、net10 / x86-64-v4、1024 要素の合計):**

- **List ソース: 0.83 倍**(253.7 → 210.2ns)— 分岐の本命。ただしガード付き devirtualization が差の大半を詰めている
- 配列ソース: 利得なし(213.8 vs 209.8ns)— 現代 JIT はプロファイルに基づくガード付き devirtualization で配列の IEnumerable 列挙を既に最適化している
- **分岐が外れる列挙子ソースへのペナルティ: 1.13 倍**(486.7 vs 552.0ns、信頼区間非重複)— 遅延イテレータ引数は使われない型テスト分を払うため、入力が List/配列主体の場面で採用する

**AOT:** ✅ 問題なし。AOT には実行時プロファイル由来の devirtualization がないため、**配列分岐も含めて JIT 環境より価値が高い**

**実装例:**

```csharp
public static int Sum(IEnumerable<int> source)
{
    if (source is int[] array)          // net10 JIT では利得なしだが AOT・保険として害もない
    {
        return SumSpan(array);
    }

    if (source is List<int> list)       // net10 でも約 1.8 倍
    {
        return SumSpan(CollectionsMarshal.AsSpan(list));
    }

    var total = 0;
    foreach (var value in source)       // フォールバック(型チェックのペナルティは実測ゼロ)
    {
        total += value;
    }

    return total;
}
```

**ユースケース:** コレクションユーティリティ、シリアライザ・マッパーの入力受け取り、LINQ 風演算子。

**関連:** 事前容量確保には `TryGetNonEnumeratedCount`(.NET 6+)で同じ分岐思想を適用できる(列挙せずに件数取得 → `new List<T>(count)` / COL-01 SetCount へ接続)。

**リポジトリ内実装:** [ベンチマーク](benchmarks/PerformancePatterns.Benchmarks/Lab/EnumerableDispatchBenchmark.cs) / [測定結果](benchmarks/results/COL-05-EnumerableDispatch.md)

---

### 🗃️ COL-06: コレクション変換の形状特化

**目的:** コレクション変換(map / copy)で、変換先の確保とコピーの方式を「入力の形状」と「出力の型」に合わせて最適化する。

**効果:**

- 件数が分かるなら容量を事前確保して成長再確保を消す(`TryGetNonEnumeratedCount` / `ICollection<T>.Count`)
- `ImmutableArray` は件数既知なら `CreateBuilder(count)` + **`MoveToImmutable()` で最終コピーを省ける**(`ToImmutable()` はコピーが入る)
- 既存インスタンスへ詰め直す場合は `EnsureCapacity` + `Clear()` で**確保済み容量を保ったまま再利用**できる(`Clear` は容量を維持するため 2 回目以降は無確保)
- 具象型(`List<T>` / `HashSet<T>`)と分かっているならインターフェース経由にせず具象のまま呼ぶ(脱仮想化・インライン化が効く)

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// ✅ 件数既知 → 容量確保 + SetCount + Span 直書き(COL-01)
var list = new List<TDestination>(count);
CollectionsMarshal.SetCount(list, count);
var destination = CollectionsMarshal.AsSpan(list);
for (var i = 0; i < source.Length; i++)
{
    destination[i] = Convert(source[i]);
}

// ✅ ImmutableArray は件数既知なら MoveToImmutable でコピーを省く
var builder = ImmutableArray.CreateBuilder<T>(count);
// ... 全要素を Add ...
var immutable = builder.MoveToImmutable();

// ✅ 既存コレクションの再利用(容量を保ったまま詰め直す)
existing.Clear();
existing.EnsureCapacity(count);
```

**ユースケース:** オブジェクトマッパー、DTO 変換、生成コードのコレクション変換部。

**関連:** 入力形状の判定は COL-05(具象型ディスパッチ)、要素書き込みは COL-01(SetCount + AsSpan)。

**実測結果(net10 / x86-64-v4):**

ImmutableArray 構築(16 要素):

| 方式 | 時間 | 割り当て |
|---|---:|---:|
| **配列から `ToImmutableArray()`** | **4.0 ns** | 88 B |
| Builder + `ToImmutable()` | 14.3 ns | 176 B |
| Builder + `MoveToImmutable()` | 11.3 ns | 88 B |

**連続領域(配列/Span)が既にあるなら `ToImmutableArray()` の一括コピーが圧勝**(Builder の要素ごと Add がボトルネック)。Builder は「要素が 1 個ずつしか得られない」場合の手段であり、そのときは `MoveToImmutable` で割り当てが半減する(176 B → 88 B)。

List 詰め直し(16 要素 / 256 要素):

| 方式 | 16 要素 | 256 要素 | 割り当て |
|---|---|---|---|
| `new List()`(容量なし、基準) | 1.00 | 1.00 | 216 B / 2,232 B |
| `new List(capacity)` | 0.51 | 0.81 | 約半減 |
| 再利用(Clear + EnsureCapacity) | 0.63 | 0.70 | **0 B** |
| **再利用 + SetCount + Span 直書き(COL-01)** | **0.19** | **0.26** | **0 B** |

→ [測定結果](benchmarks/results/COL-06-CollectionConvert.md)

**注意:** `MoveToImmutable` は **Count と Capacity が完全一致している必要**がある(不足・超過で例外)。件数が確定しない場合は `ToImmutable()` を使う。

---

## 🔤 TXT: 文字列・フォーマット

### 🔤 TXT-01: ルックアップテーブルによる整形・変換

**目的:** 数値→文字列(10 進 2 桁、Hex 等)の整形を、事前計算テーブルからのコピーに置き換える。

**効果(実測例):** DateTime の固定書式(`yyyyMMddHHmmss`)UTF-8 化で、2 桁テーブル方式は `ToString` + `Encoding.GetBytes` の約 1/3 の時間(0.34 倍)。`Utf8Formatter.TryFormat` よりさらに約 2 倍速い。

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// 00〜99 の 2 桁 ASCII を静的テーブル化(byte[100 * 2])
private static readonly byte[] DigitTable = CreateDigitTable();

[MethodImpl(MethodImplOptions.AggressiveInlining)]
private static void Write2(Span<byte> destination, int value)
    => DigitTable.AsSpan(value * 2, 2).CopyTo(destination);

// 変換テーブルは u8 リテラルでアセンブリのデータセクションを直接参照(配列確保なし)
private static ReadOnlySpan<byte> HexTable => "0123456789ABCDEF"u8;
```

**ユースケース:** 日時・数値の固定書式化、Hex/Base 系エンコーダ、プロトコル定数出力。

**補足:** `static ReadOnlySpan<byte>` プロパティ + u8 リテラル(または `new byte[] {...}` 直返し)はコンパイラがデータセクション直参照に変換するため、静的テーブルの定義方法として常にこれを既定とする。

**リポジトリ内実装:** [Utf8DateTimeFormatter.cs](src/PerformancePatterns/Txt/Utf8DateTimeFormatter.cs) / [テスト](tests/PerformancePatterns.Tests/Txt/Utf8DateTimeFormatterTest.cs) / [ベンチマーク](benchmarks/PerformancePatterns.Benchmarks/Txt/Utf8DateTimeFormatterBenchmark.cs) / [測定結果](benchmarks/results/TXT-01-Utf8DateTimeFormatter.md)

**実測結果(net10 / x86-64-v4、yyyyMMddHHmmss):** `ToString` + `Encoding.GetBytes` 比で 0.41 倍(約 2.5 倍高速)・56B → 0B、コードサイズ約 10KB → 0.9KB。`DateTime.TryFormat` + エンコードは `ToString` 比 0.90 倍と小幅な改善で、テーブル方式はそのさらに約 2.2 倍速い。

---

### 🔤 TXT-02: 文字列構築の stackalloc ファースト化

**目的:** 短命の文字列組み立てを、stackalloc 初期バッファ + プールフォールバック方式に置き換える。

**効果(実測例):** 32 文字 × 4 連結で、容量指定なし `StringBuilder` 53.4ns に対し、容量指定 19.7ns(2.7 倍)、ValueStringBuilder / pooled builder / stackalloc 付き補間ハンドラ 13.2〜13.7ns(約 4 倍)。

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// 補間文字列ハンドラに stackalloc 初期バッファを渡す
var handler = new DefaultInterpolatedStringHandler(0, 0, null, stackalloc char[128]);
handler.AppendLiteral(name);
handler.AppendFormatted(value);
var result = handler.ToStringAndClear();
```

ValueStringBuilder(stackalloc 初期バッファ + ArrayPool 拡張の ref struct)は BCL 内部実装と同型で、BUF-03 / BUF-05 の文字列特化形として自前実装する価値がある。

**リポジトリ内実装:** [ValueStringBuilder.cs](src/PerformancePatterns/Txt/ValueStringBuilder.cs) / [テスト](tests/PerformancePatterns.Tests/Txt/ValueStringBuilderTest.cs) / [ベンチマーク](benchmarks/PerformancePatterns.Benchmarks/Txt/ValueStringBuilderBenchmark.cs) / [測定結果](benchmarks/results/TXT-02-ValueStringBuilder.md)

**実測結果(net10 / x86-64-v4、24 文字 × 4 連結):** 容量指定なし `StringBuilder` 比で ValueStringBuilder は 0.30 倍(約 3.4 倍高速)、アロケーションは 760B → 216B(結果文字列のみ)。stackalloc 付き補間ハンドラとほぼ同速で、容量指定 `StringBuilder`(0.45 倍)よりさらに速い。

**ユースケース:** ログメッセージ、キー文字列生成、SQL/パス等の短文組み立て。

**注意:**

- 最低限、`StringBuilder` を使う場合も必ず容量を指定する(それだけで 2.7 倍)
- Grow 処理は JIT-04 に従い NoInlining で分離する

---

### 🔤 TXT-03: Try パターンによる例外回避

**目的:** 失敗が正常系に含まれる処理(パース・変換・検索)を、例外ではなく bool 戻り値で扱う。

**効果(実測例):**

- `int.Parse` + try/catch は成功時ですら `TryParse` の約 2.5 倍遅い。失敗時は約 540 倍(1,222ns vs 2.27ns)+ 464B のアロケーション
- 例外スロー 1 回のコストは数 μs 規模で、周辺の最適化効果を完全に飲み込む(実測例: 変換失敗パスでは 4.6 倍のキャッシュ最適化差が完全に消えた)

**AOT:** ✅ 問題なし

**設計指針:**

- ライブラリの公開 API は `TryXxx(out T result)` を正とし、例外版(`Xxx`)は Try 版のラッパーとして提供する
- 内部実装でも BCL の Try 系 API(`int.TryParse`, `Utf8Parser.TryParse` 等)を使い、try/catch を制御フローにしない

**実測結果(net10 / x86-64-v4、不正入力 10% の整数パース):** 例外制御フロー 132.5 ns/回 + 48 B(**例外 1 回 ≒ 1.3 μs**)に対し、TryParse は **2.89 ns / 0 B(0.02 倍 = 約 46 倍)**。コードサイズも 8,348 B vs 1,705 B(EH の足場分)。→ [測定結果](benchmarks/results/TXT-03-TryPattern.md)

---

### 🔤 TXT-04: バイト列トークンの直接判定

**目的:** 受信バイト列中の既知トークン(HTTP メソッド、プロトコルキーワード等)を、string 化せずバイト列のまま判定する。

**効果(実測、net10 / x86-64-v4、4 バイトトークン × 64 判定):**

- string 化 + switch 比で **0.26 倍(3.8 倍高速)+ アロケーション 2,048B → 0B** — 効果の本質は「string 化の回避」
- `SequenceEqual("GET "u8)` 連鎖と uint 定数比較は**同速**(84.1 vs 82.6ns、信頼区間重複)。net10 の SequenceEqual は定数長で十分最適化されており、uint 化の利得はコードサイズ減(226B → 166B)のみ

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// 既定: u8 リテラルの SequenceEqual(可読・安全・十分速い)
if (span.SequenceEqual("GET "u8)) { return HttpMethod.Get; }

// 分岐が多い場合・コードサイズ重視: 整数化して定数比較
// 定数は手書き 16 進でなく u8 リテラルから生成(static readonly は Tier1 で JIT 定数化)
private static readonly uint GetToken = BinaryPrimitives.ReadUInt32LittleEndian("GET "u8);

var value = BinaryPrimitives.ReadUInt32LittleEndian(span);
if (value == GetToken) { return HttpMethod.Get; }
```

**ユースケース:** プロトコルパーサーのメソッド/キーワードディスパッチ、マジックナンバー判定。

**注意:** 4/8 バイト厳密長でないトークン混在時は長さ判定を先に行う。5〜7 バイトは `ulong` 読み + マスクで対応できるが、まず SequenceEqual で書いて計測してから最適化する。

---

### 🔤 TXT-05: Utf8.TryWrite による UTF-8 直接整形

**目的:** UTF-8 出力の組み立てを、string 補間 → エンコードの 2 段ではなく、UTF-8 補間ハンドラ(.NET 8+)で `Span<byte>` へ直接書き込む。

**効果(実測、net10 / x86-64-v4、`id={int}&name={string}&ts={long}` の整形):**

- string 補間 + `Encoding.UTF8.GetBytes` 比で **0.45 倍(2.2 倍高速)+ 104B → 0B**
- char ベースの `MemoryExtensions.TryWrite` + エンコード(0.52 倍)よりも速い(中間 char 表現が不要)

**AOT:** ✅ 問題なし(補間ハンドラはコンパイル時変換)

**実装例:**

```csharp
using System.Text.Unicode;

if (Utf8.TryWrite(destination, $"id={id}&name={name}&ts={timestamp}", out var written))
{
    writer.Advance(written);
}
```

**ユースケース:** HTTP/プロトコルレスポンスの組み立て、UTF-8 ログ出力、BUF-02(IBufferWriter)への直接整形。

**注意:** 固定書式の数値・日時のみなら TXT-01(ルックアップテーブル)がさらに速い。可変フォーマット・混在コンテンツの汎用手段として使い分ける。

---

### 🔤 TXT-06: ASCII 特化比較

**目的:** ASCII 前提が保証できるトークン(HTTP ヘッダ名等)の大文字小文字無視処理を、Unicode 対応の汎用実装から ASCII 特化(.NET 8 の `Ascii` クラス)へ置き換える。

**効果(実測、net10 / x86-64-v4、ヘッダ名 8 ペアの大小無視比較):**

- `Ascii.EqualsIgnoreCase`(byte 列同士)は `string.Equals(OrdinalIgnoreCase)` 比 **0.76 倍**。byte のままで比較でき、事前の string 化自体も不要になる(実質の差はさらに大きい)
- 手書き `| 0x20` 正規化比較は 0.59 倍と最速だが、`@` と `` ` `` など記号ペアを誤同一視する罠がある

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// 既定: Ascii クラス(.NET 8+。byte/char 混在オーバーロードもある)
if (Ascii.EqualsIgnoreCase(headerName, "content-type"u8)) { ... }

// 英字のみ大小が異なる閉じたトークン集合に限り、| 0x20 正規化も可
```

**ユースケース:** HTTP ヘッダ・プロトコルフィールドの判定、`Ascii.ToLowerInPlace` 等による正規化、`char.IsAsciiDigit` 系での分類。

**注意:** 入力が非 ASCII を含みうる場合は `Ascii.IsValid` で先に判定するか、汎用実装へフォールバックする。`| 0x20` は記号衝突(`@`↔`` ` ``、`[`↔`{` 等)があるため既知集合との比較専用。

---

### 🔤 TXT-07: string.Create / TryFormat / ISpanFormattable

**目的:** 文字列生成を「確保済みバッファへの直接書き込み」にして、中間文字列・中間配列を作らない。

**効果:**

- `string.Create(length, state, action)` は最終的な文字列バッファを直接埋めるため、生成される string は 1 個だけになる(state 渡しで static ラムダにできる → DSP-04)
- `TryFormat` / `ISpanFormattable` は `ToString()` の中間 string を作らずに `Span<char>` / `Span<byte>` へ書ける
- 自作型に `ISpanFormattable` / `IUtf8SpanFormattable` を実装すると、補間ハンドラ(TXT-02)や `Utf8.TryWrite`(TXT-05)から中間表現なしで整形される

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// ✅ 文字列を 1 回の確保で組み立てる(クロージャなし)
var key = string.Create(prefix.Length + 1 + name.Length, (prefix, name), static (span, state) =>
{
    state.prefix.CopyTo(span);
    span[state.prefix.Length] = ':';
    state.name.CopyTo(span[(state.prefix.Length + 1)..]);
});

// ✅ 自作型は ISpanFormattable を実装して中間 string を作らせない
public readonly struct Measure : ISpanFormattable
{
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        => value.TryFormat(destination, out charsWritten, format, provider);
}
```

**ユースケース:** キー生成、ID 文字列、ログメッセージ、シリアライザの値整形。

**関連:** 固定書式ならテーブル方式(TXT-01)がさらに速い。可変フォーマットは TXT-02 / TXT-05 と組み合わせる。

**実測結果(net10 / x86-64-v4、prefix:name:id の組み立て):**

| 方式 | 時間 | 比率 | 割り当て |
|---|---:|---|---:|
| 文字列補間(基準) | 17.0 ns | 1.00 | 80 B |
| `string.Concat` + `ToString` | 19.6 ns | 1.15 | 176 B |
| `StringBuilder`(容量指定) | 15.6 ns | 0.92 | 280 B |
| ValueStringBuilder(TXT-02) | 12.5 ns | 0.74 | 80 B |
| **`string.Create`** | **9.4 ns** | **0.55** | **80 B** |

80 B は結果文字列そのもの(=これ以上減らせない下限)。`string.Create` は結果 1 個ぶんの確保だけで最速。→ [測定結果](benchmarks/results/TXT-07-StringCreate.md)

**注意:** `string.Create` は**長さを事前に確定できる**ことが前提。確定できない場合は TXT-02(ValueStringBuilder)を使う。

---

### 🔤 TXT-08: SearchValues\<T\>

**目的:** 「複数候補のいずれか」を探す検索を、候補集合に最適化された実装(SIMD・ビットマップ・ルックアップ)へ委譲する(.NET 8+)。

**効果:**

- 候補数が多いほど有利。`IndexOfAny(char[])` の素朴な実装より大きく速くなる
- `SearchValues.Create` の結果を **static readonly でキャッシュ**するのが前提(呼び出しごとに作ると意味がない)
- .NET 9 では文字列集合の `SearchValues<string>` も利用できる

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// ✅ 候補集合は static readonly で 1 回だけ構築する
private static readonly SearchValues<char> Delimiters = SearchValues.Create(",;:\t|");

var index = span.IndexOfAny(Delimiters);
```

**ユースケース:** トークナイザの区切り検出、エスケープが必要な文字の検出、検証(許可文字集合の判定)。

**実測結果(net10 / x86-64-v4、256 文字走査、`IndexOfAny(char[])` 比):**

| 候補数 | 配列オーバーロード | SearchValues | 比率 |
|---:|---:|---:|---|
| 3 | 5.66 ns | 5.46 ns | 0.96 |
| 8 | 13.9 ns | 4.61 ns | **0.33** |
| 32 | 23.1 ns | 4.54 ns | **0.20** |

SearchValues は**候補数に関係なく約 4.5〜5.5 ns で一定**(配列版は候補数に比例して悪化)。コードサイズも 623 B vs 約 3,960 B と小さい。→ [測定結果](benchmarks/results/TXT-08-SearchValues.md)

**注意:** **候補が 2〜3 個の場合は `IndexOfAny(char, char)` 等の専用オーバーロードの方が速い**(実測済み、不採用一覧 R-07)。上表のとおり配列オーバーロードに対しては候補 3 個でも SearchValues が優位なので、「専用オーバーロードが使える個数なら専用、それ以外は SearchValues」で使い分け、配列オーバーロードは使わない。

---

### 🔤 TXT-09: 固定長整形の応用イディオム

**目的:** 固定長フィールドの整形・トリムを、BCL のベクトル化済み API へ寄せて最小コストにする。

**効果:**

- **数値の左詰めは `TryFormat` + `Fill`**: `TryFormat` は内部で桁数計算と 2 桁テーブル書き込みを最適化済み。書いた残りを `Fill` するだけで左詰め固定長になる
- **トリムのベクトル化**: 固定長フィールドのフィラー除去は手ループでなく `IndexOfAnyExcept` / `LastIndexOfAnyExcept` を使い、切り出しは 1 回の `Slice` にする
- **UTF-16 の無変換コピー**: .NET の内部表現が UTF-16 のため、UTF-16 固定長フィールドは `MemoryMarshal.Cast<byte, char>` で Encoding を通さず memcpy 相当にできる(Cast のゼロコスト性は BIT-04 で実測済み)。パディングも char 単位の `Fill` で一括

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// ✅ 数値の左詰め固定長: TryFormat + Fill(これが最速)
value.TryFormat(buffer, out var written);
buffer[written..].Fill(Filler);

// ✅ 固定長フィールドのトリム(ベクトル化済み API を使う)
var start = field.IndexOfAnyExcept(Filler);
var end = field.LastIndexOfAnyExcept(Filler);
var trimmed = start < 0 ? [] : field[start..(end + 1)];
```

**実測結果(net10 / x86-64-v4、8 桁数値 → 12 文字フィールド / 32 文字トリム):**

| 方式 | 時間 | 比率 |
|---|---:|---|
| **`TryFormat` + `Fill`** | **3.33 ns** | 1.00 |
| 手書き LSB 書き → Reverse | 10.7 ns | 3.21(❌) |
| 手書き右詰め → 前方シフト | 12.2 ns | 3.67(❌) |
| トリム: 手書きループ | 4.50 ns | 1.00 |
| **トリム: `IndexOfAnyExcept`** | **3.80 ns** | **0.85** |

**手書きの桁順トリック(右詰め→シフト、逆順書き込み)は net10 では逆効果**(R-16 として不採用)。これらは `TryFormat` / `ISpanFormattable` 整備以前の世代の技法で、現在は BCL の桁整形が最適化済みのため上回れない。→ [測定結果](benchmarks/results/TXT-09-FixedFieldFormat.md)

**ユースケース:** 固定長レコード(帳票・EDI・レガシー連携)、プロトコルの固定幅フィールド、ID 整形。

**注意:** UTF-16 の無変換コピーはエンディアンと文字集合の前提を固定できる場合のみ。外部仕様との互換が必要なら明示変換を使う(SEQ-02 と同じ注意)。

---

## 🔄 ASY: 非同期

### 🔄 ASY-01: async ステートマシンの省略

**目的:** 内側の Task / ValueTask を加工せず返すだけのメソッドでは `async`/`await` を書かず、そのまま返す(async 消去)。

**効果(実測、net10 / x86-64-v4、同期完了パス):**

- Task 直接返し: **0.16 倍(6.4 倍高速)+ 73B → 0B** — async ラッパーはキャッシュ済みの完了 Task ですら毎回新しい Task に再ラップして確保する
- ValueTask でも await ラッパー 4.23ns に対し直接返し 0.83ns(0.13 倍 vs 0.67 倍。アロケーションはどちらも 0)

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// ❌ 単純フォワードに async/await(ステートマシン + 結果の再ラップ)
public async Task<int> ReadAsync(byte[] buffer) => await inner.ReadAsync(buffer);

// ✅ そのまま返す(async 消去)
public Task<int> ReadAsync(byte[] buffer) => inner.ReadAsync(buffer);
```

**適用条件(重要):** 「await が 1 箇所・その結果を直後に返すだけ・`try`/`using`/`lock` スコープをまたがない」単純フォワードに限定する。スコープをまたぐ場合に省略すると、例外時・解放タイミングの意味論が変わる(await 前に同期例外が呼び出し側へ直接飛ぶ、using が完了前に解放される等)。省略したメソッドは非同期スタックトレースからも消える。

**ユースケース:** デコレータ・ラッパー層のフォワードメソッド、インターフェース実装の委譲、キャッシュヒット時の完了済み Task 返し。

---

### 🔄 ASY-02: System.Threading.Channels による生産者消費者

**目的:** スレッド間のデータ受け渡しを、自前のロック+キュー+シグナルではなく `Channel<T>` で構成する。

**効果(実測、net10 / x86-64-v4、10,000 要素のパンプ):**

- Unbounded チャネルで**要素あたり約 39ns**(書き込み+非同期読み取り+完了通知込み)、アロケーションほぼゼロ
- `SingleReader`/`SingleWriter` オプションは 0.89 倍(信頼区間非重複)— 単一生産者消費者でも小幅ながら実差が出る。トポロジが固定なら宣言する
- Bounded(容量 128)は 1.63 倍 — バックプレッシャ(メモリ上限保証)の対価として妥当か判断する

**AOT:** ✅ 問題なし

**実装例:**

```csharp
var channel = Channel.CreateUnbounded<Work>(new UnboundedChannelOptions
{
    SingleReader = true, // 構成が確定しているなら宣言しておく(害はない)
    SingleWriter = false,
});

// 生産者: await channel.Writer.WriteAsync(work);  完了時 channel.Writer.Complete();
// 消費者: await foreach (var work in channel.Reader.ReadAllAsync()) { ... }
```

**ユースケース:** バックグラウンド処理キュー、ログ・メトリクスの集約、パイプライン化されたステージ間接続。

**注意:** Unbounded は生産過剰時にメモリが際限なく増える。上限保証が必要なら Bounded + `FullMode`(Wait/Drop系)を選び、2 倍のコストを織り込む。

---

### 🔄 ASY-03: System.IO.Pipelines

**目的:** ストリーム I/O の読み書きを `Pipe`(PipeReader/PipeWriter)で接続し、バッファ管理・部分読み・バックプレッシャを基盤に任せる。

**効果(実測、net10 / x86-64-v4、4KB × 16 チャンクの同スレッド転送):**

- `MemoryStream` 往復比で時間は 2.2 倍(同期機構のコスト)だが、**アロケーション 128.2KB → 1.8KB(1/70)** — プール化されたセグメント再利用の効果
- 本領は「ネットワーク/ファイル I/O のストリーミング処理」であり、メモリ内の小データ移送に使うものではない

**AOT:** ✅ 問題なし(NuGet パッケージ System.IO.Pipelines)

**ユースケース:** ソケット受信のフレーミング(長さプレフィックス・行分割)、Kestrel 型のプロトコル処理、大きなストリームの逐次パース(SEQ-01 と接続)。

**注意(実際に踏んだ罠):** 既定 `PauseWriterThreshold` は 64KB で、未消費データがこれに達すると `FlushAsync` が読み手の消費を待って**完了しなくなる**。「書き切ってから読む」逐次構造は 64KB ちょうどでデッドロックする — 書き手と読み手は必ず並行させる(本リポジトリの検証でも初版がこのデッドロックを踏んだ)。

---

### 🔄 ASY-04: IAsyncEnumerable のコスト認知と使い分け

**目的:** `await foreach` の要素あたりオーバーヘッドを把握し、同期で列挙できるデータに async ストリームを使わない。

**効果(実測、net10 / x86-64-v4、同期完了する 1,024 要素の列挙):**

- 同期 `foreach` 0.48ns/要素に対し、`await foreach` は **7.3ns/要素(15.2 倍)** — ステートマシンと `MoveNextAsync` の ValueTask 機構のコスト
- 絶対値は 7ns 程度なので、要素ごとの処理が重い(数百 ns〜)ストリームでは希釈されて問題にならない

**AOT:** ✅ 問題なし

**設計指針:**

- 同期に列挙できるなら `IEnumerable<T>` / Span 系を返す。`IAsyncEnumerable<T>` は「要素の生成自体が非同期」(ページング API、DB カーソル、ソケット)の場合に使う
- キャンセルは `[EnumeratorCancellation]` 付き `CancellationToken` 引数で受け、呼び出し側は `WithCancellation` で渡す
- ライブラリ内部の転送には ASY-02(Channels)の `ReadAllAsync` が同型の消費 API を提供する

---

### 🔄 ASY-05: ValueTask / IValueTaskSource

**目的:** 同期完了することが多い非同期 API で、`Task` オブジェクトの確保をなくす。

**効果:**

- `ValueTask<T>` は同期完了時に**ヒープ確保ゼロ**(結果を構造体に載せて返す)。キャッシュヒット・バッファ充足時に返す API で効く
- 非同期完了が繰り返し発生する高頻度パス(ソケット読み書き等)では `IValueTaskSource` を実装して**待機オブジェクト自体を再利用**できる(BCL の Socket / PipeReader が採用)
- ASY-01(async 消去)と組み合わせると、フォワード層のコストもゼロにできる

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// ✅ キャッシュヒット時は同期完了(確保なし)、ミス時のみ非同期
public ValueTask<Entry> GetAsync(string key, CancellationToken cancel)
{
    if (cache.TryGetValue(key, out var entry))
    {
        return new ValueTask<Entry>(entry);
    }

    return LoadAsync(key, cancel);   // async メソッドはこちらだけ
}
```

**ユースケース:** キャッシュ付き取得、バッファ済みストリーム読み取り、条件付き I/O。

**注意(ValueTask の利用規約):**

- **await は 1 回だけ**。複数回 await・`.Result` の複数回参照・並行 await は未定義動作。複数回必要なら `AsTask()` で変換する
- 戻り値として使う型であり、フィールドに保持して使い回すものではない
- `IValueTaskSource` の自前実装は状態管理が難しい(`ManualResetValueTaskSourceCore<T>` を土台にする)。**明確なボトルネックが実測されてから**着手する

**実測結果(net10 / x86-64-v4、同期完了 1 回あたり):**

| 方式 | 時間 | 割り当て |
|---|---:|---:|
| `Task.FromResult`(BCL キャッシュ外の値) | 2.87 ns | 72 B |
| **`new ValueTask<int>(value)`** | **0.93 ns** | **0 B** |
| async メソッド(Task 返し、同期完了) | 6.45 ns | 72 B |
| **async メソッド(ValueTask 返し、同期完了)** | **4.23 ns** | **0 B** |

同期完了するたびに Task は 72 B を確保し続けるが、ValueTask はゼロ。async 消去(ASY-01)と併用すればフォワード層のコストも消える。→ [測定結果](benchmarks/results/ASY-05-ValueTask.md)

---

### 🔄 ASY-06: 単一ループ型スケジューラ

**目的:** ジョブごとに `Timer` を作らず、1 本の待機ループで全ジョブの発火を管理する。

**効果:**

- タイマーオブジェクトとそのコールバック登録が「ジョブ数ぶん」から「1 個」になる
- 次回発火時刻を計算して 1 回だけ待つため、アイドル時のタイマー割り込みも最小になる
- ジョブ追加・削除は「待機の起こし直し」で表現できる(TaskCompletionSource を差し替えてから旧 TCS を完了させる)

**AOT:** ✅ 問題なし

**実装例(骨格):**

```csharp
private TaskCompletionSource wakeup = new(TaskCreationOptions.RunContinuationsAsynchronously);

private async Task RunLoopAsync(CancellationToken cancel)
{
    while (!cancel.IsCancellationRequested)
    {
        var delay = CalculateNextDelay();                  // 上限でクランプする
        await Task.WhenAny(wakeup.Task, Task.Delay(delay, cancel)).ConfigureAwait(false);
        FireDueJobs();
    }
}

public void Notify()
{
    // ✅ 新しい TCS に差し替えてから旧 TCS を完了させる(取りこぼし防止)
    var previous = Interlocked.Exchange(ref wakeup, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
    previous.TrySetResult();
}
```

**ユースケース:** ジョブスケジューラ、リトライ管理、TTL 期限切れ処理、バッチのフラッシュ制御。

**設計上の要点:**

- **待機時間には上限を設ける**(例: 1 時間)。長時間待機はシステム時刻変更やドリフトの影響を受けやすい
- 発火判定用の時刻取得は SYS-01(TickCount64 / GetTimestamp)に従い単調クロックを使う
- 発火対象リストは毎周 `Clear()` して使い回す。継続は `ContinueWith(static (t, state) => ..., this, ...)` の static + state 形(DSP-04)にする
- 期日情報がビット集合で表現できるなら BIT-03(TrailingZeroCount)で次候補を O(1) 取得できる(cron 実装の定石)

**実測結果(net10 / x86-64-v4、プリミティブ単位):** ジョブごとの `Timer` 生成 + 破棄 36.0 ns + 120 B(グローバルタイマーキューへの登録を含む)に対し、単一ループ方式の起床通知(TCS 差し替え + `TrySetResult`)は **20.3 ns + 88 B(0.56 倍)**。登録・通知プリミティブの比較であり、負荷時のスケジューラ全体挙動は対象外。→ [測定結果](benchmarks/results/ASY-06-SchedulerPrimitive.md)

**注意:** 生産者消費者としての受け渡しが主目的なら ASY-02(Channels)の方が適する。

---

### 🔄 ASY-07: ストリーミング I/O

**目的:** 送受信データを一度メモリに溜めず、到着・生成したそばから流す。

**効果:**

- レスポンス全体のバッファリングが消え、ピークメモリが「全体サイズ」から「チャンクサイズ」になる
- 大きなペイロードで LOH 確保・GC 圧力を回避できる
- 先頭から処理できるため、体感レイテンシ(最初のバイトまでの時間)も改善する

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// ✅ ヘッダ到着時点で復帰し、本文は逐次読む
using var response = await client
    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel)
    .ConfigureAwait(false);

await using var stream = await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false);
await ParseAsync(stream, cancel).ConfigureAwait(false);   // 全体を byte[] 化しない

// ✅ 送信側は HttpContent 内で直接ストリームへ書く(中間 MemoryStream を作らない)
protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    => JsonSerializer.SerializeAsync(stream, value, jsonTypeInfo);
```

**設計上の要点:**

- **長さが事前に分かるなら `TryComputeLength` / `Content-Length` を返す**(受け側がバッファ確保でき、chunked のオーバーヘッドも避けられる)。分からない場合のみ chunked にする
- 圧縮ストリームで包むときは `leaveOpen: true` にして、下位ストリームを閉じずに圧縮側だけ確実に flush する
- 進捗通知が不要なら `Stream.CopyToAsync` に丸投げする(ランタイム最適化済み)。必要なときだけ ArrayPool を借りた手動ループにする

**関連:** より本格的なフレーミングは ASY-03(Pipelines)、自前の固定バッファ運用は SEQ-04(リングバッファ)。

**実測結果(net10 / x86-64-v4、1 MB ペイロードの処理):** 全体を byte[] へバッファしてから処理 395.5 μs + **2,097,484 B(Gen0/1/2 GC 発生 = LOH 圧力)** に対し、ArrayPool の 16 KB チャンク逐次処理は **224.5 μs + 64 B(0.58 倍、GC ゼロ)**。ピークメモリが「ペイロード全体」から「チャンクサイズ」へ落ち、キャッシュ局所性で速度も改善する。→ [測定結果](benchmarks/results/ASY-07-StreamBuffering.md)

**注意:** 逐次処理はエラー時に「途中まで送信済み」の状態が発生しうるため、リトライ設計とセットで考える。

---

## 🔒 CON: 並行・同期

### 🔒 CON-01: Interlocked によるワンショットガード

**目的:** 「一度だけ実行する」制御(Dispose の多重呼び出し防止、べき等初期化)を、lock ではなく `Interlocked` の 1 命令で行う。

**効果(実測、net10 / x86-64-v4、破棄済みパスの定常コスト):**

| 方式 | 時間 | コードサイズ | 正確に 1 回の保証 |
|---|---|---|:---:|
| 素の bool | 0.18 ns | 26 B | ❌(単一スレッド前提) |
| lock(System.Threading.Lock) | 8.85 ns | 2,612 B | ✅ |
| **Interlocked.Exchange / CompareExchange** | **3.90 / 3.98 ns** | **33 / 56 B** | ✅ |

スレッド安全な排他同士の比較で、`Interlocked` は **lock 比 2.2〜2.3 倍高速・コードサイズ約 1/50**。

**AOT:** ✅ 問題なし

**実装例:**

```csharp
private int disposed;

public void Dispose()
{
    if (Interlocked.CompareExchange(ref disposed, 1, 0) != 0)
    {
        return;     // 2 回目以降は即帰る
    }

    // 解放処理(1 回だけ実行される)
}

// べき等初期化(結果を待つ必要がない fire-once)
private static int initialized;

public static void EnsureInitialized()
{
    if (Interlocked.Exchange(ref initialized, 1) == 1)
    {
        return;
    }

    // 初期化処理
}
```

**ユースケース:** Dispose ガード、グローバル初期化、フラグの CAS 制御。

**注意:**

- **スレッド安全が不要な型(単一スレッド前提の ref struct 等)では素の bool が最速(0.40ns)でありそれで十分。** このパターンは「スレッド安全な排他が必要な場合に lock を使わない」ための選択
- `Interlocked` に bool オーバーロードはないため `int`(0/1)で表現する。bool 風に包むなら true を -1(全ビット 1)で持つ AtomicBoolean 形が定石
- `Exchange` 版は「初期化完了を待たずに帰る」ことに注意。完了待ちが必要なら `Lazy<T>` または lock + 二重チェックを使う

---

## 🖥️ SYS: システム・OS 機能

### 🖥️ SYS-01: 低コストの時刻・経過時間取得

**目的:** キャッシュ TTL・タイムアウト判定・経過時間測定で、壁時計時刻(`DateTime.UtcNow`)の取得コストを回避する。

**効果(実測、net10 / x86-64-v4):**

| API | 時間 | 比率 | 特性 |
|---|---|---|---|
| `DateTime.UtcNow` / `DateTimeOffset.UtcNow` | 21.5 / 21.7 ns | 1.00 | 壁時計時刻。システム時刻変更の影響を受ける |
| `Stopwatch.GetTimestamp` | 16.0 ns | 0.75 | 高分解能・単調。`Stopwatch.GetElapsedTime` で TimeSpan 化 |
| `Environment.TickCount64` | **1.08 ns** | **0.05(20 倍)** | 単調・ミリ秒単位(分解能 ~10〜16ms) |

**AOT:** ✅ 問題なし

**設計指針:**

- TTL・タイムアウト判定(ms 精度で十分)→ `Environment.TickCount64` の差分比較
- 高精度な経過時間測定 → `Stopwatch.GetTimestamp` + `Stopwatch.GetElapsedTime`
- 実際の日時が必要な場合のみ `DateTime.UtcNow`(判定用途に壁時計を使うとシステム時刻変更で誤動作するため、正しさの面でも単調クロックが優る)
- テスト容易性が必要な箇所は `TimeProvider`(.NET 8+)で抽象化し、ホットパスのみ直接 API を使う

**ユースケース:** キャッシュの有効期限、レートリミッタ、リトライ・タイムアウト管理、簡易メトリクス。

---

## 🗄️ DAT: データアクセス

### 🗄️ DAT-01: DB アクセスの列解決最適化

**目的:** `DbDataReader` から POCO へのマッピングで、行ごと・列ごとに繰り返される解決コストを読み取り開始前へ寄せる。

**効果:**

- **序数の事前解決**: 列名から序数を引く処理を行ごとに行わず、リーダー 1 本につき 1 回だけ確定する。序数を保持する `readonly struct` を作って `in` で渡せば、行マッピングは構造体フィールドの読み出しになる(MEM-04)
- **1 パス列解決**: `GetOrdinal(name)` を列数ぶん呼ぶ代わりに、`GetName(i)` を昇順に 1 回走査して全序数を確定する。欠落列を -1 のまま扱えるので、部分列 SELECT でも例外にならない(`GetOrdinal` は例外を投げる)
- **型別リーダーメソッド**: `GetValue` + アンボックスではなく `GetInt32` / `GetString` 等を直接呼ぶ。enum は基底型で読んでキャストする
- **結果セット形状のキャッシュ**: 「列名 + 列型」の組み合わせをキーにマッパーをキャッシュする(同じ POCO でも SELECT 列が違えば別マッパー)

**AOT:** ✅ 問題なし(生成コード or 手書きの場合。リフレクション/Emit ベースのマッパーは [aot-compatibility.md](docs/aot-compatibility.md) の AOTP-01/06 を参照)

**実装例:**

```csharp
// ✅ 序数は 1 回だけ解決して readonly struct に畳む
private readonly struct Ordinals(int id, int name, int createdAt)
{
    public readonly int Id = id;
    public readonly int Name = name;
    public readonly int CreatedAt = createdAt;
}

// ✅ GetName の 1 パス走査で全列を解決(欠落は -1 のまま)
static Ordinals ResolveOrdinals(DbDataReader reader)
{
    int id = -1, name = -1, createdAt = -1;
    for (var i = 0; i < reader.FieldCount; i++)
    {
        var column = reader.GetName(i);
        if (String.Equals(column, "Id", StringComparison.OrdinalIgnoreCase)) { id = i; }
        else if (String.Equals(column, "Name", StringComparison.OrdinalIgnoreCase)) { name = i; }
        else if (String.Equals(column, "CreatedAt", StringComparison.OrdinalIgnoreCase)) { createdAt = i; }
    }

    return new Ordinals(id, name, createdAt);
}

// ✅ 初行で序数を確定してからループ本体へ(毎行の「初回か?」判定を消す)
if (reader.Read())
{
    var ordinals = ResolveOrdinals(reader);
    do
    {
        list.Add(Map(reader, in ordinals));
    }
    while (reader.Read());
}
```

**列名照合の戦略選択:** 列数に応じて `String.Equals(OrdinalIgnoreCase)` の連鎖(少数)と サンプリングハッシュ switch(中〜多数)を使い分ける(COL-04 / BIT-01)。生成コードなら列数が生成時に分かるため出し分けられる。

**CommandBehavior の選択:**

| 指定 | 効果 | 注意 |
|---|---|---|
| `SequentialAccess` | 行全体をバッファせず前方のみ読む | **列を序数の昇順で読む必要がある**。プロパティ宣言順に読む実装では使えない |
| `SingleResult` / `SingleRow` | 不要な結果セット・行の準備を省く | 単一行/単一結果と分かっている場合のみ |
| `SchemaOnly` | 行を転送せずスキーマだけ取得 | 列型の事前解決に有効 |
| `CloseConnection` | リーダー破棄時に接続も閉じる | 自分で開いた接続のみ |

**実測結果(net10 / x86-64-v4、3 列 × 1000 行のインメモリリーダー、1 行あたり):**

| 方式 | 時間/行 | 比率 | 割り当て/行 | コードサイズ |
|---|---:|---|---:|---:|
| 毎行 `GetOrdinal` × 3(基準) | 7.45 ns | 1.00 | 0 B | 2,219 B |
| **序数キャッシュ struct + `in` 渡し** | **1.00 ns** | **0.13** | 0 B | 533 B |
| 序数キャッシュ + `GetValue` + キャスト | 4.26 ns | 0.57 | **48 B(❌ ボックス化)** | 1,169 B |

序数解決を行ループの外へ出すだけで約 8 倍。型別メソッド(`GetInt32` 等)を使わず `GetValue` に頼ると、行ごとに値型のボックス(int + bool = 48 B)が積み上がる。プロバイダの仮想ディスパッチと I/O は含まない(解決戦略の差分のみを分離して測定)。→ [測定結果](benchmarks/results/DAT-01-OrdinalResolve.md)

**注意:** 手書きで全部やるより、Source Generator で生成する方が保守しやすい。

---

## 🏭 GEN: コード生成

### 🏭 GEN-01: Emit 生成コードの高速化戦略

**目的:** 実行時 IL 生成(`DynamicMethod` / `TypeBuilder`)を使う場合に、生成される**コードそのもの**を速くする。

**効果:**

- **子ファクトリのインライン展開**: 生成デリゲートが別の生成デリゲートを呼ぶ構造だと呼び出しが連鎖する。子の構築手順を記録しておき、親の IL へ直接展開すると呼び出しが消える(展開量には上限を設ける)
- **定数埋め込み**: 依存が解決時に確定している(シングルトン等)なら、ファクトリ呼び出しではなくホルダーのフィールド読み出しだけを生成する
- **Holder 型のフィールドをデリゲートのターゲットにする**: 必要な個数のフィールドだけを持つ型を生成し `CreateDelegate(type, holder)` で束ねると、IL は `Ldarg_0; Ldfld` の直接フィールドアクセスになる(`object[]` の添字もクロージャ参照も不要)
- ~~具象デリゲート型の Invoke を `Call` で呼ぶ~~: **net10 では効果なしを確認**(生成コードが完全一致 — 下記実測)。ターゲットフィールドの読み出しが null チェックを兼ねるため、JIT が `callvirt` のチェックを消す
- **IL サイズの最小化**: `Ldc_I4_0..8` / `Ldarg_0..3` など短縮オペコードを値域で選ぶ

**AOT:** ❌ **非互換**。Reflection.Emit は Native AOT で `PlatformNotSupportedException`([aot-compatibility.md](docs/aot-compatibility.md) の AOTP-01)

**適用条件:** JIT 環境専用の高速パスとして実装し、AOT では静的フォールバックへ切り替える二重パス構成(AOTS-08)にする。

```csharp
public static Func<T> CreateFactory<T>() where T : new()
{
    // IsDynamicCodeCompiled で判定する(インタープリタ環境では Emit が逆に遅い)
    if (RuntimeFeature.IsDynamicCodeCompiled)
    {
        return EmitFactoryBuilder.Build<T>();
    }

    return static () => new T();
}
```

**設計指針:** **新規開発では Source Generator(AOTS-01)を第一選択**とする。Emit は「既存資産の維持」「利用者のコード生成が不可能な動的シナリオ」に限り、上記のフォールバックとセットで使う。

**実測結果(net10 / x86-64-v4、2 依存のファクトリ呼び出し):**

| ターゲット戦略 | 時間 | 比率 |
|---|---:|---|
| C# クロージャラムダ(参照値) | 3.77 ns | 1.00 |
| **Holder フィールド(`ldfld` 直読み)** | **4.23 ns** | **1.12(ほぼ同等)** |
| closure 配列(`ldelem` + `castclass`) | 4.61 ns | 1.22(❌) |
| 子ファクトリ連鎖(Callvirt) | 6.36 ns | 1.69(❌) |
| 子ファクトリ連鎖(Call) | 6.46 ns | 1.71(❌) |

Holder フィールドターゲットはコンパイル済みクロージャに肉薄する(同じ `ldfld` 形になるため)。closure 配列は 1.2 倍、子デリゲート連鎖は 1.7 倍のペナルティ — **インライン展開と Holder 化が効く**という主張は実測で確認。

一方 `Call` vs `Callvirt` 置換は計測 6.36 vs 6.46 ns で信頼区間が重なったため、判定ポリシーに従い **JitDisasm で生成コードを比較 → 68 命令・229 バイトが完全一致**。デリゲート Invoke ではターゲットフィールドの読み出し(`mov rcx, [delegate+0x08]`)がハードウェア null チェックを兼ねるため、`callvirt` のチェックが JIT で消える。よって**誤差ではなく「差なし」— net10 のデリゲート Invoke に対してこの置換は効果がない**(R-17 として不採用一覧に記録)。→ [測定結果](benchmarks/results/GEN-01-EmitStrategy.md)

**注意:** 生成コードの検証は通常のテストでは漏れやすいため、生成物の等価性テストを必ず用意する。

---

### 🏭 GEN-02: Source Generator 生成コードの設計

**目的:** Source Generator(AOTS-01)で**どのようなコードを生成すればパフォーマンスを実現できるか**を確定させる。ジェネレータの実装方法ではなく、出力すべきコードの形の指針。

**設計の 3 原則:**

1. **実行時解決をビルド時へ移す** — 辞書引き・リフレクション・ハッシュ計算・文字列組み立てを、定数・switch・直書き `new` に焼き込む
2. **件数・形状で出し分ける** — ジェネレータは対象の件数・型を知っている。実行時ライブラリにはできない「N に応じた実装切り替え」を生成時に行う
3. **測定済みパターンで構成する** — 生成コードの中身は本カタログの採用パターンのみ。不採用(R-01〜R-17)を含めない

**AOT:** ✅ 問題なし(AOT 対応の根本手段そのもの)

**シナリオ → 生成すべき形(要約。コード例と根拠は [生成コードパターン集](docs/generated-code-patterns.md)):**

| シナリオ | 生成すべき形 | 根拠実測 |
|---|---|---|
| 名前 → インデックス解決 | ≤4 件は Equals 連鎖 / ≥5 件はサンプリングハッシュ switch(定数焼き込み) | COL-04 / R-07 |
| 型別成果物(SQL・型名・キー) | const / static readonly / `"..."u8` へ直書き | TYP-06(0.09 ns) |
| DB 行マッパー | 序数 struct + `in` 渡し + 型別 getter(`GetValue` は生成しない) | DAT-01(0.13 倍) |
| ファクトリ / DI | 依存グラフを `new` 直書きへインライン展開(子ファクトリ連鎖を生成しない) | GEN-01(連鎖 2.3 倍) |
| 整形・シリアライズ | `TryFormat` 直呼び + u8 リテラル + `string.Create` + テーブル(TXT-01) | TXT-01 / 05 / 07、R-16 |
| enum 特化 | 名前スイッチの適用 + ToString は switch 定数返し | COL-04 に帰着 |
| コレクション変換 | 容量確定 + `SetCount` + Span 直書きループ | COL-01 / COL-06 |
| 変更通知・イベント | EventArgs の static readonly 焼き込み + 購読数に応じた形 | DSP-03 / DSP-04 |

**❌ 生成してはいけないコード(アンチ生成):** typeof キャッシュ(R-01)、無条件 Frozen 化(R-08)、性能目的 readonly(R-10)、CopyBlock 置換(R-14)、手書き桁詰め(R-16)、Call 置換(R-17)、手動 ref 走査(R-02)、自前ハッシュループ(BIT-04)、実行時 Type キー辞書の主経路化(TYP-01)。一覧と理由は[生成コードパターン集](docs/generated-code-patterns.md)の第 9 節。

**GEN-01 との関係:** 直書き生成コードは Emit の最良形(Holder フィールド 6.55 ns ≒ クロージャ 6.23 ns)と同等の形を AOT 安全に出力できる。Emit 併設(AOTS-08 の二重パス)が要るのは「ビルド時に生成できない動的シナリオ」だけ。

**注意:** 生成コードにも等価性テストと本書の検証プロセス(誤差ポリシー含む)を適用する。

---

## 🤖 net10 では手書き不要になった最適化(ランタイム自動化の記録)

かつて有効だった(または有効とされてきた)手書き最適化のうち、**現行ランタイム(.NET 10)が自動で行うため書く必要がなくなったもの**の一覧。生成コード・AI によるコード生成では、これらを「速くするため」に出力しないこと(可読な素直な形で書けば同じコードになる)。

| 手書きしていた形 | ランタイムが自動でやること | 確認方法 | 記録 |
|---|---|---|---|
| `(uint)(v - min) <= max - min` の範囲チェック変換 | JIT が 2 比較形を符号なし 1 比較へ自動融合 | Tier1 生成コード一致 | [R-18](benchmarks/results/LAB-RangeCheck.md) |
| 定数 2 の累乗サイズの `%` を `&` マスクへ書き換え | JIT が定数剰余を AND 形へ畳み込み | 実測でマスクと同水準 | [BIT-02](benchmarks/results/BIT-02-PowerOfTwoMask.md) |
| 小さなヘルパーへの `AggressiveInlining` 付与 | 既定ポリシー(PGO)がループ持ちでもインライン化 | 呼び出し側 Tier1 コード一致(94 B) | [JIT-01](benchmarks/results/JIT-01-Inlining.md) |
| `new T[0]` を `Array.Empty<T>()` へ置換 | 空配列を共有化し割り当てゼロに(コードサイズは `[]` が小) | BDN 割り当てゼロ実測 | [STK-07](benchmarks/results/STK-07-LazyAllocation.md) |
| 単一 Span ループの `GetReference` + `Unsafe.Add` 化 | 標準 for で境界チェックを完全除去 | 手動化は 1.07〜1.13 倍遅 | [R-02](docs/rejected-patterns.md) |
| **複数 Span** の単純ループの手動 ref 走査 | 索引形を自動ベクトル化(0.36 ns/要素) | 手動化はベクトル化を阻害し 1.46 倍遅 | [R-02](benchmarks/results/LAB-DualSpanWalk.md) |
| 配列走査の `GetArrayDataReference` + `Unsafe.Add` 化 | 索引形で境界チェック除去 + 自動ベクトル化 | 逐次で 1.30 倍遅、ランダムでも差なし | [R-02](benchmarks/results/LAB-ArrayDataReference.md) |
| 速度目的の関数ポインタ `delegate*` 化 | デリゲートは PGO が推測脱仮想化 + インライン化 | 関数ポインタは calli で投機不可、6.04 倍遅 | [DSP-02](benchmarks/results/DSP-02-CallAbstraction.md) |
| `typeof(X)` の static readonly キャッシュ | Tier1 で凍結 RuntimeType ポインタの即値へ定数化 | 生成コード完全一致(11 B) | [R-01](docs/rejected-patterns.md) |
| ループ構文の書き分け(for / while) | 同一の命令列へ正規化 | 生成コード一致(28 B) | [R-04](docs/rejected-patterns.md) |
| デリゲート Invoke の `call` 置換(null チェック回避) | ターゲットフィールド読みが null チェックを兼務 | 生成コード完全一致(229 B) | [R-17](docs/rejected-patterns.md) |
| 手書きの桁整形ループ(右詰めシフト・逆順書き) | `TryFormat` 内部が桁数計算・2 桁テーブル・アンロール済み | TryFormat の 2.5〜4.8 倍遅 | [R-16](docs/rejected-patterns.md) |
| 自前ハッシュループ(FNV-1a 等) | `string.GetHashCode` / XxHash3 がベクトル化済み | 64 文字以降で自前が逆転負け | [BIT-04](benchmarks/results/BIT-04-XxHash3.md) |
| エスケープしないボックスの回避 | エスケープ解析がスタック化(0.004 ns) | STK-05 実測例 | [STK-05](#-stk-05-ボックス化回避と頻出値キャッシュ) |
| 末尾要素の事前タッチによる境界チェック誘導 | ヒントなしで同一(全バリアント差なし) | net10 / net8 実測 | [R-15](docs/rejected-patterns.md) |

**読み方:** ここに載っている形は「書いても壊れない」が、**書く理由がもうない**。手書きが依然必要な境界(複合条件の範囲チェック、実行時サイズのマスク、複数 Span の同時走査、エスケープするボックス等)は各パターン本文・不採用記録の「代わりにやること」を参照。

---

## ⚠️ AOT では前提が変わる項目

本書の実測はすべて **JIT(net10、Dynamic PGO 有効)** で取得している。Native AOT には JIT も PGO も存在しないため、**「PGO の投機的最適化に依存して速かった形」は AOT で優位性を失う**。AOT を主戦場にする場合は以下を再評価する。

| 項目 | JIT(PGO あり)での実測 | AOT で起きること | AOT 向けの指針 |
|---|---|---|---|
| デリゲート vs 関数ポインタ(DSP-02) | デリゲート 1.74 倍 < 関数ポインタ 6.04 倍 | デリゲートの推測付き脱仮想化・インライン化が効かなくなり、両者の差は縮む | 関数ポインタの不利は JIT 固有。AOT では改めて計測する |
| インターフェース経由の呼び出し(DSP-01) | sealed の有無で差なし(PGO が単相を推測) | 投機がないぶん実際の仮想ディスパッチが残る | **具象 sealed 型で保持**する形の価値が JIT より大きい |
| 小さなヘルパーのインライン化(JIT-01) | 既定ポリシーが自動でインライン化(属性は差なし) | プロファイルなしの静的ヒューリスティクスのみ | `AggressiveInlining` を明示する価値が JIT より大きい |
| `AggressiveOptimization` | Dynamic PGO を無効化するため**かえって遅くなりうる** | 階層型コンパイルがないため無意味(無害) | JIT では原則使わない。AOT では付けても意味がない |
| 実行時コード生成(GEN-01) | Emit の最良形はコンパイル済みと同等 | `PlatformNotSupportedException`(AOTP-01) | Source Generator(GEN-02)へ置換する |

**逆に AOT で有利になる項目:** 起動時の階層コンパイル待ちがないため、TYP-04 / TYP-06 のような型初期化子ベースのキャッシュや静的テーブルは初回から最適化済みで動く。R-01(typeof キャッシュ)が JIT の Tier1 昇格前に不利だった問題も AOT では発生しない。

詳細な非互換パターンと対策は [aot-compatibility.md](docs/aot-compatibility.md) を参照。

---

## ❌ 採用しない手法(不採用一覧)

実測で効果なし・逆効果と判定した手法。**コード生成・レビューでこれらを「最適化」として適用しないこと。** 各手法の狙い・実測・代替の詳細は [docs/rejected-patterns.md](docs/rejected-patterns.md) を参照。

| ID | 手法 | 不採用の理由(一言) |
|---|---|---|
| R-01 | `typeof(X)` の static readonly キャッシュ | JIT が typeof を定数化するため完全に同速 |
| R-02 | 手動 ref 走査(GetReference / GetArrayDataReference) | どの形状でも速くならない(複数 Span 1.46 倍・配列 1.30 倍遅い) |
| R-03 | `CollectionsMarshal.AsSpan` 後の手動 ref ウォーク | 差なし、コードサイズ増のみ |
| R-04 | ループ構文の選択(for / while / do-while / 昇降順) | 差なし |
| R-05 | class 要素配列への ArrayPool 適用 | 要素個別の確保が残り効果なし〜逆効果 |
| R-06 | 自前ソート実装 | BCL の `Span.Sort` が約 9 倍速い |
| R-07 | 候補 2〜3 文字での `SearchValues` | `IndexOfAny(char, char)` 専用オーバーロードが速い |
| R-08 | `FrozenDictionary` の無条件採用 | 構築 15〜20 倍、キー集合次第で検索も逆転 |
| R-09 | Span で書ける処理の `fixed` ポインタ化 | 同速か遅い(固定コストが載る) |
| R-10 | readonly フィールド化による JIT 最適化の期待 | インライン化される限り差は測定不能 |
| R-11 | static メソッド直バインドデリゲートの保持 | thunk 経由で最も遅い呼び出し形態になりうる |
| R-12 | 反復目的の ref フィールドカーソル(C# 11) | Span + for に勝てず 1.21 倍 |
| R-13 | 性能目的の pinned(POH)バッファ化 | fixed は実測無料、POH 確保は 17.5 倍 + Gen2 |
| R-14 | `Span.CopyTo` の `Unsafe.CopyBlockUnaligned` 置換 | 可変長で同速、定数長も微差のみ |
| R-15 | 末尾要素の事前タッチによる境界チェック誘導 | net10 / net8 とも全バリアント差なし |
| R-16 | 手書きの桁順整形トリック(右詰め→シフト・逆順書き込み) | TryFormat + Fill の 2.5〜4.8 倍遅い |
| R-17 | デリゲート Invoke の Call 置換(Callvirt 回避) | 生成コード完全一致を JIT 確認(net10) |
| R-18 | 符号なしオーバーフローによる範囲チェックの手書き | JIT が 2 比較を自動融合、生成コード実質同一 |

---

## 🚫 Source Generator で生成すべきでない実装

生成コード(および AI によるコード生成)が「速くなりそう」という理由で出力しがちだが、**実測・生成コード確認により効果なし〜逆効果と確定済み**の形。GEN-02 の設計原則 3「測定済みパターンのみで構成する」の具体的な禁止リスト。

| 生成してはいけない形 | 理由(実測) | 代わりに生成する形 | 記録 |
|---|---|---|---|
| `typeof(X)` の static readonly キャッシュ | Tier1 で生成コード完全一致。昇格前はキャッシュ側が不利 | `typeof(X)` を直書き | R-01 |
| 単一 Span ループの `GetReference` + `Unsafe.Add` 化 | 標準 for で境界チェック除去済み。1.07〜1.13 倍遅 | 索引形の `for` | R-02 |
| 手動 ref 走査(単一 / 複数 Span・配列) | 索引形は境界チェック除去 + 自動ベクトル化済み。手動化は複数 Span で 1.46 倍遅 | 索引形の `for` | [R-02](docs/rejected-patterns.md) |
| 配列走査の `GetArrayDataReference` 化 | 逐次で 1.30 倍遅、ランダムでも差なし | 索引形の `for` | [MEM-02](#-mem-02-struct-要素配列--ref-アクセスデータ指向レイアウト) |
| 読み取り専用辞書の無条件 `FrozenDictionary` 化 | 構築 7.4〜10.2 倍、検索利得なし(string キー) | `Dictionary` か名前スイッチ(COL-04) | R-08 |
| インスタンスフィールドへの性能目的 `readonly` | 生成コード同一(オフセット以外) | 設計意図としてのみ付与 | R-10 |
| `Span.CopyTo` の `Unsafe.CopyBlockUnaligned` 置換 | 可変長は同じ Memmove へ到達(誤差) | `CopyTo` | R-14 |
| 手書きの桁詰め整形ループ(右詰めシフト・逆順書き) | `TryFormat` + `Fill` の 2.5〜4.8 倍遅 | `TryFormat` + `Fill` | R-16 |
| デリゲート Invoke の `call` 置換(`callvirt` 回避) | 生成コード 68 命令・229 B 完全一致 | `callvirt` のまま | R-17 |
| 速度目的の関数ポインタ `delegate*` 化 | calli は PGO 投機不可・インライン不可で 6.04 倍遅 | 具象 sealed 型 or デリゲート | [DSP-02](#-dsp-02-呼び出し抽象化の選択指針) |
| 自前ハッシュループ(FNV-1a 等) | 64 文字以降 `string.GetHashCode` より遅い | XxHash3(BIT-04)かサンプリング(BIT-01) | [BIT-04](#-bit-04-xxhash3-による汎用ハッシュ) |
| 実行時 `Type` キー辞書を主経路にする生成 | 実行時 Type 経路は素の Dictionary より 1.93 倍遅 | ジェネリック API で受けて静的解決(TYP-01) | [TYP-01](#️-typ-01-静的型スロットtypemap--typeslot) |
| `(T)Enum.Parse(typeof(T), name)` 形 | 非ジェネリック版はボックス化 + 常に確保 | `Enum.TryParse<T>` か名前スイッチ | [STK-05](#-stk-05-ボックス化回避と頻出値キャッシュ) |

シナリオ別の「生成すべきコード形」と根拠は [生成コードパターン集](docs/generated-code-patterns.md)、不採用の詳細は [rejected-patterns.md](docs/rejected-patterns.md) を参照。

---

## 🔍 逆引き:目的別の選択指針

| 目的 | 推奨パターン |
|---|---|
| ループ内の境界チェック除去 | 索引形で書く(手動 ref 走査は R-02 で不採用) |
| スタックフレーム初期化コスト削減 | MEM-01 |
| 関数呼び出しコスト削減 | JIT-01 |
| 比較・検索の仮想呼び出し除去 | JIT-02 |
| 範囲チェックの分岐削減 | 手書き不要 — JIT が自動融合(R-18) |
| 既知キー集合(列挙型名等)の高速ハッシュ | BIT-01 |
| 一時オブジェクトのヒープ確保禁止 | STK-01 |
| コピーなしのデータ参照 | STK-02 |
| foreach のアロケーション除去 | STK-03 / STK-04 |
| 小さなバッファのアロケーション排除 | BUF-03(stackalloc) |
| 中〜大バッファの GC 回避 | BUF-01 / BUF-04 |
| 出力バッファへの直接書き込み | BUF-02 |
| バイナリ・テキストの逐次読み書き | SEQ-01 |
| テキスト/バイナリ分割 | SEQ-01 |
| Stream との構造体 I/O | SEQ-02 |
| 全体をマテリアライズしないシーケンス処理 | SEQ-03 |
| 型ベースマップの高速読み取り | TYP-01 |
| 値型の辞書キー比較 | TYP-02 |
| 非公開メンバーへのリフレクションなしアクセス | TYP-03 |
| 内部データ構造のアロケーション排除 | MEM-02 |
| ホットループ内のスライス | MEM-03 |
| ジェネリック変換の型別特殊化 | JIT-03 / TYP-04 |
| ホットパスのインライン化促進 | JIT-04 |
| ハッシュ表のインデックス計算 | BIT-02 |
| コールバック・ファクトリの保持形態選択 | DSP-01 / DSP-02 |
| 複数購読イベントの高速発火 | DSP-03 |
| object 境界のボックス化回避 | STK-05 |
| 一時バッファの確保戦略 | BUF-05 |
| List/Dictionary の内部直接アクセス | COL-01 |
| 不変辞書の検索高速化 | COL-02 |
| Span キーでの辞書検索 | COL-03 |
| 名前→値解決の実装選択 | COL-04 / BIT-01 |
| 固定書式の整形・進数変換 | TXT-01 |
| 短命文字列の組み立て | TXT-02 |
| パース・変換の失敗ハンドリング | TXT-03 |
| 型保証済みキャストの高速化 | TYP-05 |
| ビットマップ走査・ビット計数 | BIT-03 |
| 単純フォワードの async 排除 | ASY-01 |
| TTL・タイムアウト用の時刻取得 | SYS-01 |
| ラムダのキャプチャ排除 | DSP-04 |
| 使用時まで確保しない設計 | STK-07 |
| Dispose・初期化の多重実行防止 | CON-01 |
| 大きな構造体の引数渡し | MEM-04 |
| 構造体内に固定長領域を持つ | STK-08 |
| 可変長引数の配列確保除去 | STK-09 |
| 参照型インスタンスの再利用 | BUF-07 |
| ストリーミング受信のレコード分割 | SEQ-04 / ASY-03 |
| コレクション変換の確保・コピー最適化 | COL-06 |
| 文字列生成のゼロアロケーション化 | TXT-07 |
| 多数候補の文字検索 | TXT-08(候補 2〜3 個は専用オーバーロード) |
| 固定長フィールドの整形・トリム | TXT-09 |
| 汎用ハッシュ(長い入力・安定値) | BIT-04 |
| 型ごとの文字列・SQL の事前確定 | TYP-06 |
| パイプライン・コールバックの合成コスト削減 | DSP-05 |
| 同期完了が多い非同期 API | ASY-05 |
| 多数ジョブの時刻管理 | ASY-06 |
| 大きなペイロードの送受信 | ASY-07 |
| DB リーダーの列解決 | DAT-01 |
| Emit 生成コード自体の高速化 | GEN-01(AOT 非互換) |
| Source Generator に何を生成させるか | GEN-02([生成コードパターン集](docs/generated-code-patterns.md)) |

---

## 🛠️ Unsafe / MemoryMarshal API 早見表

低レベル API は複数パターンに分散しているため、用途からの横断参照用にまとめる。

| API | 用途 | 関連パターン |
|---|---|---|
| `Unsafe.Add(ref r, i)` | ref からのオフセットアクセス(境界チェックなし) | R-02(構造上の用途のみ) |
| `Unsafe.As<T>(object)` | 型チェック省略キャスト(参照型) | TYP-05 |
| `Unsafe.As<TFrom, TTo>(ref v)` | ref の再解釈(ジェネリック特殊化・ビット再解釈) | JIT-03 / SEQ-02 |
| `Unsafe.ReadUnaligned / WriteUnaligned` | アラインメント非保証位置の unmanaged 読み書き | SEQ-01 / SEQ-02 / BUF-02 |
| `Unsafe.SkipInit(out v)` | out 変数の初期化スキップ | MEM-01 / SEQ-02 |
| `Unsafe.SizeOf<T>()` | unmanaged 型のサイズ(JIT 定数) | SEQ-01 / SEQ-02 |
| `Unsafe.IsAddressLessThan` | ref 同士の位置比較(終端判定) | R-02(構造上の用途のみ) |
| `Unsafe.BitCast<TFrom, TTo>`(.NET 8+) | 同サイズ値型の安全なビット再解釈(As の安全版) | SEQ-02 / TYP-02 |
| `MemoryMarshal.GetReference(span)` | Span 先頭への ref 取得 | R-02(構造上の用途のみ) |
| `MemoryMarshal.GetArrayDataReference(array)` | 配列先頭への ref 取得 | R-02(構造上の用途のみ) |
| `MemoryMarshal.Cast<TFrom, TTo>(span)` | Span の要素型再解釈(ゼロコスト) | TYP-02 / 拡充候補 XxHash3 |
| `MemoryMarshal.AsBytes(span)` | Span の byte ビュー化 | TYP-02 |
| `MemoryMarshal.CreateSpan(ref r, len)` | ref からの Span 構築 | SEQ-02 |
| `CollectionsMarshal.AsSpan(list)` | List 内部配列の Span 化 | COL-01 |
| `CollectionsMarshal.GetValueRefOrAddDefault` | 辞書エントリへの ref 取得 | COL-01 |
| `RuntimeHelpers.IsReferenceOrContainsReferences<T>()` | 参照有無の型別分岐(JIT 定数) | JIT-05 |

**共通の注意:** これらは境界チェック・型安全性を自分で保証する API 群。公開 API の入力検証を通過した後の内部実装に閉じて使い、Debug ビルドでの `Debug.Assert` 併用を推奨する。

---

## 📖 ドキュメント構成

| ドキュメント | 内容 |
|---|---|
| README.md(本書) | パフォーマンス実装パターンの分類・一覧・解説(主知識) |
| [docs/rejected-patterns.md](docs/rejected-patterns.md) | 採用しない手法の詳細(なぜ効果がない・逆効果か) |
| [docs/aot-compatibility.md](docs/aot-compatibility.md) | AOT / トリミング対応パターン一覧(非互換パターンと対策) |
| [docs/benchmark-methodology.md](docs/benchmark-methodology.md) | ベンチマーク実施ガイドライン(BenchmarkDotNet 構成と測定の落とし穴) |
| [docs/generated-code-patterns.md](docs/generated-code-patterns.md) | Source Generator 生成コードパターン集(何を生成すれば速いか・アンチ生成リスト) |

## 🏗️ リポジトリ構成

```
dotnet-performance/
├── README.md                          パターンカタログ(本書)
├── docs/                              補助ドキュメント(AOT / 不採用手法 / 測定手法)
├── src/PerformancePatterns/           パターン実装(カテゴリ別フォルダ、パターン ID を XML ドキュメントに記載)
├── tests/PerformancePatterns.Tests/   実装の正しさの検証(xunit)
└── benchmarks/
    ├── PerformancePatterns.Benchmarks/  BenchmarkDotNet による効果実証(Lab/ は検証用)
    └── results/                         測定結果の記録(パターン ID 対応、英語)
```

- 実装・テスト・ベンチマークはパターン ID(例: SEQ-01)で本書と対応付ける
- ベンチマークは [docs/benchmark-methodology.md](docs/benchmark-methodology.md) の規約(実行前 Verify・インターン回避・既定 net10 単独)に従う
- AOT 対応の詳細 ID(AOTP-xx / AOTS-xx)は [docs/aot-compatibility.md](docs/aot-compatibility.md) を参照

## 🌱 拡充候補パターン(今後追加予定)

現時点の候補はすべて本文へ収録済み。新しい候補は以下の観点で追加し、実装例・ベンチマークによる検証を経て「実装例」欄を埋めていく。

- 新しいランタイム / 言語機能(.NET・C# のリリースごと)
- 実運用ライブラリのコードから抽出した実装イディオム
- 検証キューで「条件付き」と判定され、条件の切り分けが未完了のもの

**未計測パターンの扱い:** 本文中に「本リポジトリ未計測」と明記したパターンは、実装例フェーズで測定してから実測値を追記する。数値の裏付けがない主張はカタログとして弱いため、採用判断の前に必ず計測する。
