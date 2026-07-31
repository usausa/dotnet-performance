# .NET パフォーマンス実装パターン一覧

高速・低アロケーションな .NET ライブラリを実装するためのパターンカタログ。

- 各パターンには一意の ID を付与し、今後追加する実装例プロジェクト・ベンチマークはこの ID に対応付ける
- ライブラリ実装者、および AI にコード生成させる際の参照資料として使用する
- AOT / トリミング対応の詳細(非互換パターンと対策)は [aot-compatibility.md](aot-compatibility.md) を参照
- 効果検証のベンチマーク手順と落とし穴は [benchmark-methodology.md](benchmark-methodology.md) を参照
- 「実測例」の数値は環境・ランタイム世代で変動する目安であり、採用時は対象環境での再計測を前提とする

## AOT 対応マークの凡例

| マーク | 意味 |
|:---:|---|
| ✅ | Native AOT / トリミング環境でそのまま動作する |
| ⚠️ | 実装方法によっては AOT 非互換になる(各パターンの注記を参照) |
| ❌ | AOT では動作しない(代替手段が必要) |

本書の低レベル最適化パターンは、リフレクションや動的コード生成に依存しないため**ほぼすべて AOT 対応**である。
AOT で問題になるのは主に「柔軟性のためのリフレクション・動的コード生成」であり、それらの問題と対策パターンは [aot-compatibility.md](aot-compatibility.md) にまとめる。

---

## カテゴリ構成

| カテゴリ | 内容 |
|---|---|
| MEM | メモリアクセス最適化(境界チェック除去・データレイアウト) |
| JIT | JIT 最適化支援(インライン化・分岐除去・特殊化) |
| BIT | ビット演算・ブランチレス最適化 |
| DSP | 呼び出し抽象化・ディスパッチ |
| STK | スタック活用・ゼロアロケーション型設計 |
| BUF | バッファ管理・プーリング |
| SEQ | 逐次読み書き・シーケンス処理 |
| COL | コレクション最適化 |
| TXT | 文字列・フォーマット |
| TYP | 型システム活用(型ディスパッチ・比較・内部アクセス) |

## パターン一覧(サマリー)

| ID | パターン | 目的 | AOT | 実装例 |
|---|---|---|:---:|:---:|
| [MEM-01](#mem-01-memorymarshalgetreference--unsafeadd) | MemoryMarshal.GetReference + Unsafe.Add | 境界チェックの完全除去 | ✅ | 未着手 |
| [MEM-02](#mem-02-getarraydatareference) | GetArrayDataReference | 配列先頭への直接参照取得 | ✅ | 未着手 |
| [MEM-03](#mem-03-skiplocalsinit) | SkipLocalsInit | ローカル変数ゼロ初期化のスキップ | ✅ | 未着手 |
| [MEM-04](#mem-04-struct-要素配列--ref-アクセスデータ指向レイアウト) | struct 要素配列 + ref アクセス | 要素ごとのヒープ確保と間接参照の排除 | ✅ | 未着手 |
| [MEM-05](#mem-05-sliceoffset-length-による明示的スライス) | Slice(offset, length) 明示スライス | 範囲演算子より高速なスライス | ✅ | 未着手 |
| [JIT-01](#jit-01-aggressiveinlining--aggressiveoptimization) | AggressiveInlining / AggressiveOptimization | インライン展開・最適化の強制 | ✅ | 未着手 |
| [JIT-02](#jit-02-iequatablet-制約による分岐除去) | IEquatable\<T\> 制約による分岐除去 | 比較の仮想ディスパッチ除去 | ✅ | 未着手 |
| [JIT-03](#jit-03-typeoft-分岐によるジェネリック特殊化) | typeof(T) 分岐特殊化 | ジェネリック変換の分岐除去 | ✅ | 未着手 |
| [JIT-04](#jit-04-コールドパス分離throw-ヘルパー--grow-の-noinlining) | コールドパス分離 | ホットパスのインライン化促進 | ✅ | 未着手 |
| [BIT-01](#bit-01-符号なしオーバーフローによる範囲チェック) | 符号なしオーバーフロー範囲チェック | 範囲判定の分岐削減 | ✅ | 未着手 |
| [BIT-02](#bit-02-ドメイン制約を活かした軽量ハッシュ生成) | ドメイン制約を活かした軽量ハッシュ | 既知キー集合の O(1) ハッシュ | ✅ | 未着手 |
| [BIT-03](#bit-03-2-の累乗サイズ--マスクによる剰余置換) | 2 の累乗サイズ + マスク | 剰余(除算)のビット AND 化 | ✅ | 未着手 |
| [DSP-01](#dsp-01-sealed-による-devirtualization) | sealed による devirtualization | 仮想呼び出しの直接化 | ✅ | 未着手 |
| [DSP-02](#dsp-02-呼び出し抽象化の選択指針) | 呼び出し抽象化の選択指針 | delegate/interface/関数ポインタの使い分け | ✅ | 未着手 |
| [DSP-03](#dsp-03-ハンドラ列の不変配列化マルチキャストデリゲート回避) | ハンドラ列の不変配列化 | マルチキャストデリゲートの劣化回避 | ✅ | 未着手 |
| [STK-01](#stk-01-ref-structスタック専用型) | ref struct(スタック専用型) | ヒープエスケープの型レベル禁止 | ✅ | 未着手 |
| [STK-02](#stk-02-spant--readonlyspant-によるゼロコピーアクセス) | Span\<T\> / ReadOnlySpan\<T\> | ゼロコピーの型付きビュー | ✅ | 未着手 |
| [STK-03](#stk-03-struct-iterator-パターン) | struct iterator パターン | foreach の仮想呼び出し・ヒープ確保除去 | ✅ | 未着手 |
| [STK-04](#stk-04-static-ローカルメソッドによる-iterator-の最適化) | static ローカルメソッド iterator | 即時バリデーション + クロージャ防止 | ✅ | 未着手 |
| [STK-05](#stk-05-ボックス化回避と頻出値キャッシュ) | ボックス化回避と頻出値キャッシュ | object 境界のアロケーション排除 | ✅ | 未着手 |
| [BUF-01](#buf-01-arraypoolt-によるバッファ再利用) | ArrayPool\<T\> | 使い捨てバッファの GC 圧力削減 | ✅ | 未着手 |
| [BUF-02](#buf-02-ibufferwritert--getspan--advance-パターン) | IBufferWriter\<T\> + GetSpan / Advance | 出力バッファへの直接書き込み | ✅ | 未着手 |
| [BUF-03](#buf-03-bufferwriterslimtスタックファースト書き込み) | BufferWriterSlim\<T\> | スタックファーストのバッファ書き込み | ✅ | 未着手 |
| [BUF-04](#buf-04-memoryownertスコープ付きバッファ所有権) | MemoryOwner\<T\> | プールレンタルへの RAII スコープ付与 | ✅ | 未着手 |
| [BUF-05](#buf-05-一時バッファの段階戦略stackalloc--arraypool-統合) | 一時バッファの段階戦略 | stackalloc/プールの閾値切替統合 | ✅ | [実装](../src/PerformancePatterns/Buf/TemporaryBuffer.cs) |
| [SEQ-01](#seq-01-spanreadert--spanwritert) | SpanReader\<T\> / SpanWriter\<T\> | ゼロアロケーション逐次読み書き | ✅ | 未着手 |
| [SEQ-02](#seq-02-spantokenizert) | SpanTokenizer\<T\> | 汎用スパン分割(ゼロアロケーション) | ✅ | [実装](../src/PerformancePatterns/Seq/SpanTokenizer.cs) |
| [SEQ-03](#seq-03-stream-構造体-io) | Stream 構造体 I/O | 構造体の直接バイナリ読み書き | ✅ | 未着手 |
| [SEQ-04](#seq-04-遅延評価シーケンス処理batch--segment--traverse) | Batch / Segment / Traverse | 低アロケーションのシーケンス処理 | ✅ | 未着手 |
| [COL-01](#col-01-collectionsmarshal-による内部直接アクセス) | CollectionsMarshal | List/Dictionary 内部への直接アクセス | ✅ | 未着手 |
| [COL-02](#col-02-frozendictionary-の条件付き採用) | FrozenDictionary 条件付き採用 | 不変辞書の検索高速化 | ✅ | 未着手 |
| [COL-03](#col-03-getalternatelookup-による-span-キー検索) | GetAlternateLookup | Span キーでの辞書検索 | ✅ | 未着手 |
| [COL-04](#col-04-少数要素ルックアップの戦略選択) | 少数要素ルックアップ戦略 | 規模・形状に応じた実装選択 | ✅ | 未着手 |
| [TXT-01](#txt-01-ルックアップテーブルによる整形変換) | ルックアップテーブル整形 | 固定書式整形のテーブル化 | ✅ | 未着手 |
| [TXT-02](#txt-02-文字列構築の-stackalloc-ファースト化) | 文字列構築の stackalloc ファースト | StringBuilder 代替の低アロケーション構築 | ✅ | [実装](../src/PerformancePatterns/Txt/ValueStringBuilder.cs) |
| [TXT-03](#txt-03-try-パターンによる例外回避) | Try パターン | 例外を制御フローに使わない | ✅ | 未着手 |
| [TYP-01](#typ-01-静的型スロットtypemap--typeslot) | 静的型スロット(TypeMap / TypeSlot) | Type キー辞書の配列アクセス化 | ⚠️ | 未着手 |
| [TYP-02](#typ-02-bitwisecomparert生バイト比較) | BitwiseComparer\<T\> | unmanaged 値型の生バイト比較 | ✅ | 未着手 |
| [TYP-03](#typ-03-unsafeaccessor非公開メンバーへの直接アクセス) | UnsafeAccessor | 非公開メンバーへの直接アクセス | ✅ | 未着手 |
| [TYP-04](#typ-04-ジェネリック-static-クラスによる型別キャッシュ) | ジェネリック static 型別キャッシュ | 型ごとの成果物の辞書レス取得 | ✅ | 未着手 |
| [TYP-05](#typ-05-unsafeas-による型チェック省略キャスト) | Unsafe.As キャスト | 型保証済みキャストの高速化 | ✅ | 未着手 |

## 逆引き:目的別の選択指針

| 目的 | 推奨パターン |
|---|---|
| ループ内の境界チェック除去 | MEM-01 / MEM-02 |
| スタックフレーム初期化コスト削減 | MEM-03 |
| 関数呼び出しコスト削減 | JIT-01 |
| 比較・検索の仮想呼び出し除去 | JIT-02 |
| 範囲チェックの分岐削減 | BIT-01 |
| 既知キー集合(列挙型名等)の高速ハッシュ | BIT-02 |
| 一時オブジェクトのヒープ確保禁止 | STK-01 |
| コピーなしのデータ参照 | STK-02 |
| foreach のアロケーション除去 | STK-03 / STK-04 |
| 小さなバッファのアロケーション排除 | BUF-03(stackalloc) |
| 中〜大バッファの GC 回避 | BUF-01 / BUF-04 |
| 出力バッファへの直接書き込み | BUF-02 |
| バイナリ・テキストの逐次読み書き | SEQ-01 |
| テキスト/バイナリ分割 | SEQ-02 |
| Stream との構造体 I/O | SEQ-03 |
| 全体をマテリアライズしないシーケンス処理 | SEQ-04 |
| 型ベースマップの高速読み取り | TYP-01 |
| 値型の辞書キー比較 | TYP-02 |
| 非公開メンバーへのリフレクションなしアクセス | TYP-03 |
| 内部データ構造のアロケーション排除 | MEM-04 |
| ホットループ内のスライス | MEM-05 |
| ジェネリック変換の型別特殊化 | JIT-03 / TYP-04 |
| ホットパスのインライン化促進 | JIT-04 |
| ハッシュ表のインデックス計算 | BIT-03 |
| コールバック・ファクトリの保持形態選択 | DSP-01 / DSP-02 |
| 複数購読イベントの高速発火 | DSP-03 |
| object 境界のボックス化回避 | STK-05 |
| 一時バッファの確保戦略 | BUF-05 |
| List/Dictionary の内部直接アクセス | COL-01 |
| 不変辞書の検索高速化 | COL-02 |
| Span キーでの辞書検索 | COL-03 |
| 名前→値解決の実装選択 | COL-04 / BIT-02 |
| 固定書式の整形・進数変換 | TXT-01 |
| 短命文字列の組み立て | TXT-02 |
| パース・変換の失敗ハンドリング | TXT-03 |
| 型保証済みキャストの高速化 | TYP-05 |

---

## MEM: メモリアクセス最適化

### MEM-01: MemoryMarshal.GetReference + Unsafe.Add

**目的:** スパン要素への配列境界チェックを完全に排除する。

**効果:**

- 各アクセスから境界チェック命令を除去(JIT が自動で除去できない場合に有効)
- ループ内で唯一のポインタ演算になるため CPU の最適化効率が上がる
- 実測例: 標準インデクサーより約 10% 高速(要素 1024 個)

**AOT:** ✅ 問題なし(純粋な IL レベルの操作)

**実装例:**

```csharp
ref var head = ref MemoryMarshal.GetReference(span);
for (var i = 0; i < span.Length; i++)
{
    Process(Unsafe.Add(ref head, i));
}
```

**ユースケース:** 高頻度な内側ループ、パーサー、シリアライザ。

**適用判断(実測に基づく指針):**

| 状況 | 推奨 | 理由 |
|---|---|---|
| 複数の Span を同一ループで同時走査 | GetReference + Unsafe.Add | JIT は 2 本目以降の Span の境界チェックを除去できないことが多く、手動除去が効く(実測例: 0.82〜0.90 倍に短縮) |
| 単一 Span の標準 for ループ | 通常のインデクサ | JIT の境界チェック除去が完全に効いており、手動 ref 化はセットアップコスト分だけ遅い(実測例: 1.07〜1.13 倍に悪化) |
| 要素数が 1〜数個 | 通常のインデクサ | ref 準備コストが支配的になり逆転する |

**注意:**

- 境界チェックが消える分、範囲の正しさは呼び出し側の責任になる。終端 ref の計算ミスや複数カーソル走査での ref 進め忘れは静かに範囲外を読む(実際に混入しやすいバグ)
- 手動 ref 走査はコードサイズを増やしがちで、速くならない場面では純粋な負債になる。必ずベンチマークとセットで採用する

---

### MEM-02: GetArrayDataReference

**目的:** 配列の要素 0 への直接参照を取得する。`AsSpan()` を介さずに利用できる。

**効果:**

- `AsSpan` の一時 Span 生成コストを省略
- 型情報なしに直接 ref 演算が可能

**AOT:** ✅ 問題なし

**実装例:**

```csharp
ref var head = ref MemoryMarshal.GetArrayDataReference(array);
for (var i = 0; i < array.Length; i++)
{
    Unsafe.Add(ref head, i) = ComputeValue(i);
}
```

**ユースケース:** 配列への高速一括書き込み、テーブル初期化。

**注意:** null チェック・長さ検証は一切行われない。配列が非 null かつ範囲内であることは呼び出し側で保証する。

---

### MEM-03: SkipLocalsInit

**目的:** ローカル変数のゼロ初期化(`.locals init`)をスキップする。

**効果:**

- スタックフレーム確保時の `memset` を除去
- `stackalloc` を多用するメソッドで特に有効
- 実測で数十 ns のオーバーヘッドを削減できる場合がある

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

**注意:**

- プロジェクトに `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` が必要(unsafe コードを書かなくても属性の使用に必要)
- 未初期化領域を読まないよう、書き込み前の読み取りがないことを保証する。`Unsafe.SkipInit(out value)` との併用も検討

---

### MEM-04: struct 要素配列 + ref アクセス(データ指向レイアウト)

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

---

### MEM-05: Slice(offset, length) による明示的スライス

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

**注意:** 可読性の差はごく小さいため、ホットパスでは `Slice(offset, length)` を既定にしてよい。1 回きりのスライスでは差は誤差レベル。

---

## JIT: JIT 最適化支援

### JIT-01: AggressiveInlining / AggressiveOptimization

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

---

### JIT-02: IEquatable\<T\> 制約による分岐除去

**目的:** ジェネリック型引数に `IEquatable<T>` 制約を加え、JIT に専用の比較コードを生成させる。

**効果:**

- `EqualityComparer<T>.Default` の仮想ディスパッチが除去される
- プリミティブ型では直接の `==` 命令に展開される
- `IndexOf` 等のスパン検索は型に特化した SIMD 実装が選択される

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

---

### JIT-03: typeof(T) 分岐によるジェネリック特殊化

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

**関連する知見:** `typeof(X)` を `static readonly Type` フィールドにキャッシュする最適化は無意味(JIT が `typeof` 自体を定数化するため、実測で速度・コードサイズとも完全に同値)。可読性を優先してよい。

---

### JIT-04: コールドパス分離(Throw ヘルパー / Grow の NoInlining)

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

---

## BIT: ビット演算・ブランチレス最適化

### BIT-01: 符号なしオーバーフローによる範囲チェック

**目的:** `min <= value && value <= max` の 2 比較・2 分岐を、符号なし整数の性質を利用した 1 比較に削減する。

**効果:**

- 比較・分岐が 2 回 → 1 回になり、分岐予測ミスの機会が減る
- .NET ランタイム自身が配列境界チェック等で多用する定石で、JIT 最適化との親和性が高い
- 単発の効果は小さい(実測例: 100 ns 規模の処理で 1〜2 ns)が、ホットループ内の頻出判定で積み上がる

**AOT:** ✅ 問題なし

**実装例:**

```csharp
// Before: 比較 2 回
public static bool IsInRange(int value, int min, int max)
    => (min <= value) && (value <= max);

// After: 比較 1 回(min <= max が前提)
public static bool IsInRange(int value, int min, int max)
{
    unchecked
    {
        return (uint)(value - min) <= (uint)(max - min);
    }
}
```

**仕組み:** `value < min` の場合、`value - min` は負になり `uint` として解釈すると巨大な値に折り返す(オーバーフロー)ため、`<= (uint)(max - min)` が必ず false になる。範囲内なら差分は `max - min` 以下に収まるため、単一比較で上下限を同時に判定できる。

**ユースケース:** 連続値 enum の定義済み判定、インデックス検証、文字種判定(`(uint)(c - '0') <= 9` で数字判定等)。TYP-01 の実装例にある `(uint)index < (uint)array.Length` はこのパターンの特殊形。

**注意:**

- 可読性は明確に劣る。マイクロベンチマークで効果を確認できるホットパスに限定する
- 意図的なオーバーフロー利用であることを `unchecked` で明示する(プロジェクト設定が checked でも壊れないようにする)
- `min <= max` の成立が前提。破ると全入力が範囲外判定になる

---

### BIT-02: ドメイン制約を活かした軽量ハッシュ生成

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
- 要素アクセスは MEM-01(取得済み ref 経由)の併用で境界チェックなしにできる

**適用範囲の実測知見:**

- C# コンパイラの文字列 switch(少数なら長さ+文字判定、多数なら全文ハッシュ+ジャンプテーブルへ lowering)との比較では一律の勝者はない。少数(〜4 件)はコンパイラ生成が速く、中規模(〜12 件)や共通接頭辞で衝突しやすいキー集合ではサンプリングハッシュ switch が約 2 倍速く、大規模(32 件〜)では再びコンパイラ生成が優位になる
- 大文字小文字を無視した enum 名パースでは `Enum.TryParse`(ignoreCase)比で 0.11〜0.24 倍と圧倒的(素の文字列 switch は Ordinal になるため ignoreCase 用途に使えず、このパターンの独壇場)。数件以下なら `Equals(OrdinalIgnoreCase)` の if 連鎖(0.17 倍)で足りる
- 固定の先頭/中央/末尾サンプリングが衝突するキー集合では、コード生成(Source Generator)時に衝突しないサンプリング位置を探索して定数として埋め込む

---

### BIT-03: 2 の累乗サイズ + マスクによる剰余置換

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

**注意:**

- 符号付き int の `/ 2` や `% 2` は JIT が単純シフトに落とせない(負数補正が入る)。非負が保証できるなら uint 化または符号なし右シフト `>>>`(C# 11)を使う
- 境界チェック除去目的の無条件な uint キャスト小細工は最近のランタイムでは効果が消えていることが実測されている(BIT-01 の範囲チェックのような意味のある形に限定する)

---

## DSP: 呼び出し抽象化・ディスパッチ

### DSP-01: sealed による devirtualization

**目的:** 実装クラスを `sealed` にして、JIT が仮想呼び出し・インターフェース呼び出しを直接呼び出し+インライン化へ置き換え(devirtualization)できるようにする。

**効果:**

- sealed 型の変数経由の呼び出しは実行時型が確定するため、JIT が直接呼び出しに落とせる
- 効果はコンテキスト依存(既にインライン化やガード付き devirtualization が効いている場合は差が出ないことも実測されている)が、コストはゼロ

**AOT:** ✅ 問題なし。AOT には実行時プロファイルによるガード付き devirtualization がないため、静的に sealed で確定させる価値がむしろ大きい

**実装例:**

```csharp
public sealed class MessagePackFormatter : IFormatter { ... }
```

**設計指針:** 継承を設計意図として明示的に許すクラス以外、ライブラリの実装クラスはすべて sealed を既定とする(BCL も同方針)。

---

### DSP-02: 呼び出し抽象化の選択指針

**目的:** コールバック・ファクトリ・ストラテジの保持形態(デリゲート / インターフェース / 関数ポインタ)を、実測に基づいて選択する。

**知見(実測例):**

- 最近のランタイム(.NET 9/10)では、インターフェース / abstract 経由の呼び出しはデリゲート呼び出しと同等〜高速(100 万回で 197μs vs 227μs)。「デリゲートの方が軽い」という古い常識は成立しない
- static メソッドを直接バインドしたデリゲートは最も遅い形態になりうる(this 引数を詰め替える thunk を経由するため)。デリゲートに乗せるなら、コンパイラがキャッシュするラムダ(`static (x) => Foo(x)` 形式)の方が速いことがある
- メソッド内の小さな処理はラムダではなく static ローカル関数にする(実測例: コードサイズ 185B vs 6B、ローカル関数は完全インライン化されデリゲート生成も呼び出しも消える)
- 関数ポインタ `delegate*<T>` は `Func<T>` より軽い(コードサイズ 36B → 28B)が、JIT のインライン化・最適化消去を妨げる障壁になる場面では逆に遅くなる。適用はベンチマーク前提

**AOT:** ✅ 問題なし(マネージド関数ポインタは AOT 対応)

**ユースケース:** DI コンテナのファクトリ表、シリアライザのフォーマッタ解決、パイプラインのステージ保持。

---

### DSP-03: ハンドラ列の不変配列化(マルチキャストデリゲート回避)

**目的:** 購読者が複数になりうるイベント/コールバックを、マルチキャストデリゲート(`+=`)ではなく不変配列 + `Volatile.Read` で保持・実行する。

**効果:**

- 実測例: マルチキャストデリゲートは購読 2 個から急劣化し、購読 4 個では不変配列の foreach 実行が約 6.6 倍高速
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

**注意:** 購読 1 個が支配的な用途では単一デリゲートのままが最速。購読解除の頻度が高い場合は配列再構築コストも考慮する。

---

## STK: スタック活用・ゼロアロケーション型設計

### STK-01: ref struct(スタック専用型)

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

**注意:**

- フィールドとしてクラスに保持できない、`await` / `yield` をまたげない等の制約がある(C# 13 以降は一部緩和)
- C# 13 からは ref struct のインターフェース実装と `allows ref struct` 制約が使用可能

---

### STK-02: Span\<T\> / ReadOnlySpan\<T\> によるゼロコピーアクセス

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

---

### STK-03: struct iterator パターン

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

**注意:** struct enumerator を `IEnumerable<T>` として公開するとボックス化されて効果が消える。struct を直接返す `GetEnumerator()` を公開し、`IEnumerable<T>` 実装が必要な場合は明示的実装で分離する。

---

### STK-04: static ローカルメソッドによる iterator の最適化

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

---

### STK-05: ボックス化回避と頻出値キャッシュ

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

---

## BUF: バッファ管理・プーリング

### BUF-01: ArrayPool\<T\> によるバッファ再利用

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

---

### BUF-02: IBufferWriter\<T\> + GetSpan / Advance パターン

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

---

### BUF-03: BufferWriterSlim\<T\>(スタックファースト書き込み)

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

**注意:** stackalloc サイズは 256〜512 バイト程度を目安とし、再帰・ループ内での確保は避ける(スタックオーバーフロー対策)。

---

### BUF-04: MemoryOwner\<T\>(スコープ付きバッファ所有権)

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

**補足:** `IMemoryOwner<T>` インターフェースに準拠させると `MemoryPool<T>` 系 API と相互運用できる。非同期メソッドをまたぐ場合は ref struct にできないため class または struct で実装する。

---

### BUF-05: 一時バッファの段階戦略(stackalloc / ArrayPool 統合)

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

**リポジトリ内実装:** [TemporaryBuffer.cs](../src/PerformancePatterns/Buf/TemporaryBuffer.cs) / [テスト](../tests/PerformancePatterns.Tests/Buf/TemporaryBufferTest.cs) / [ベンチマーク](../benchmarks/PerformancePatterns.Benchmarks/Buf/TemporaryBufferBenchmark.cs) / [測定結果](../benchmarks/results/BUF-05-TemporaryBuffer.md)

**実測結果(Ryzen 9 5900X / net8〜10):** 4096 要素では `new T[]` 比 0.11〜0.32 倍(3〜9 倍高速、ゼロ初期化コストの除去)+ 0B。64 要素の stackalloc 経路は `new` より僅かに遅い(5.1ns vs 3.5ns)が 88B → 0B — **小サイズの価値は速度ではなく GC 圧力ゼロ化**にある。`ArrayPool` 直接利用と比べると小サイズで有利(stackalloc 経路がプールアクセスを回避)。

**注意:**

- 変種として `[ThreadStatic]` static バッファの使い回しがある(完全アロケーションゼロ)が、再入・async 境界をまたぐ保持・スレッドごとのメモリ滞留に注意。ThreadStatic フィールドへのアクセス自体にもコストがあるため、使う場合はループ前にローカル変数へ退避する
- stackalloc 側の閾値は 256〜512 要素程度を目安にする(BUF-03 と同様)

---

## SEQ: 逐次読み書き・シーケンス処理

### SEQ-01: SpanReader\<T\> / SpanWriter\<T\>

**目的:** `Span<T>` を逐次的に読み書きするための軽量 ref struct カーソル。

**効果:**

- 位置管理を構造体が担うため呼び出し側が offset を手動管理不要
- `ref readonly T Read()` で参照返し → コピーゼロ
- `Slide()` で書き込み先スライスを取得し、後からデータを埋めることができる(長さプレフィックス等)

**AOT:** ✅ 問題なし(`T : unmanaged` ジェネリックは AOT で完全特殊化される)

**実装例:**

```csharp
// バイナリプロトコルの解析
var reader = new SpanReader<byte>(packetSpan);
var magic   = reader.ReadUnmanaged<uint>();
var length  = reader.ReadUnmanaged<int>();
var payload = reader.Read(length);
```

**ユースケース:** バイナリプロトコル解析、ファイルフォーマットパーサー、カスタムシリアライザ。

---

### SEQ-02: SpanTokenizer\<T\>

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

**リポジトリ内実装:** [SpanTokenizer.cs](../src/PerformancePatterns/Seq/SpanTokenizer.cs) / [テスト](../tests/PerformancePatterns.Tests/Seq/SpanTokenizerTest.cs) / [ベンチマーク](../benchmarks/PerformancePatterns.Benchmarks/Seq/SpanTokenizerBenchmark.cs) / [測定結果](../benchmarks/results/SEQ-02-SpanTokenizer.md)

**実測結果(Ryzen 9 5900X / net8〜10):** `string.Split` 比で 4 トークン時 0.30〜0.34 倍(約 3 倍高速)・64 トークン時 0.62〜0.70 倍、アロケーションは 216B〜3,096B → **0B**。.NET 9+ の `MemoryExtensions.Split` と比べても 7〜26% 高速でコードサイズも小さい(548B vs 751B)。

---

### SEQ-03: Stream 構造体 I/O

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

**注意:** メモリレイアウトがそのまま外部形式になるため、`[StructLayout(LayoutKind.Sequential, Pack = 1)]` 等でレイアウトを固定し、エンディアン・パディングを設計として明示すること。異環境互換が必要な場合は `BinaryPrimitives` による明示変換を使う。

---

### SEQ-04: 遅延評価シーケンス処理(Batch / Segment / Traverse)

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

**関連:** .NET 9+ の `Enumerable.Chunk` / `Index` 等の標準 API で足りる場合はそちらを優先し、バケット再利用などの最適化が必要な場合に自前実装する。

---

## COL: コレクション最適化

### COL-01: CollectionsMarshal による内部直接アクセス

**目的:** `List<T>` / `Dictionary<TKey, TValue>` の公開 API を迂回し、内部ストレージへの Span / ref を直接取得する。

**効果(実測例):**

- `CollectionsMarshal.AsSpan(list)` で List 反復が最大 1.9 倍(.NET 8)。`List<T>` の素の foreach は最も遅い反復手段
- `GetValueRefOrAddDefault` で辞書の read-modify-write(`map[key]++` 相当)が約 1.35 倍(ハッシュ計算・探索が 2 回 → 1 回になる)

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
```

**ユースケース:** 集計処理、キャッシュのヒットカウント、内部モデルの一括更新。

**注意:**

- `AsSpan` 保持中に List へ Add しない(内部配列の差し替えで Span が古い配列を指す)
- 旨味は「読み+書きの統合」にある。追加のみなら `TryAdd` と差はなく、`GetValueRefOrNullRef` + `Unsafe.IsNullRef` の存在チェックを挟むと効果が減る

---

### COL-02: FrozenDictionary の条件付き採用

**目的:** 構築後に変化しない辞書を `FrozenDictionary` にして検索を高速化する。

**効果と適用条件(実測例):**

- 検索は `Dictionary` 比 2〜4 倍高速(1024 件)。ただし**構築は 15〜20 倍遅く**割り当ても大きい — 起動時に一度だけ構築して読み続ける用途限定
- キー集合によっては検索も逆転する(実測例: enum 名 64 件で Dictionary より 1.15〜1.31 倍遅い)。採用前に実データで計測する
- `Type` キーの辞書では専用実装(TYP-01 系の型スロット、またはオープンアドレスの型ハッシュマップ)が FrozenDictionary の約 3 倍速い
- `ReadOnlyDictionary` ラッパーはラップ分だけ確実に遅くなる(不変性の表明には `FrozenDictionary` か `IReadOnlyDictionary` 公開を使う)

**AOT:** ✅ 問題なし

**ユースケース:** 設定テーブル、キーワード辞書、静的マッピング。

---

### COL-03: GetAlternateLookup による Span キー検索

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

**注意:** comparer が `IAlternateEqualityComparer` を実装している必要がある(既定の string comparer / `StringComparer.Ordinal(IgnoreCase)` は対応済み)。`FrozenDictionary` / `HashSet` にも同 API がある。

---

### COL-04: 少数要素ルックアップの戦略選択

**目的:** 要素数・キーの性質・アクセスパターンに応じて、辞書 / 線形探索 / 分岐チェーン / ハッシュ switch を選び分ける。

**知見(実測例):**

- 〜8 件程度: `string.Equals` の if 連鎖が最速級(enum 名解決で `Enum.TryParse` 比 0.17 倍)。小規模では配列の線形探索も辞書より速い
- 十数件〜: サンプリングハッシュ switch(BIT-02)が安定して速い。Equals 連鎖は「宣言順どおりのアクセス」では速いが、逆順・部分アクセスでは 3〜5 倍劣化する — 平均ではなくアクセス形状への安定性で選ぶ
- FrozenDictionary はこの規模(〜32 件)では最速になりにくい(実測でどのカラム数でも最速にならなかった)

**AOT:** ✅ 問題なし

**ユースケース:** Source Generator が生成する名前→インデックス解決(DB カラム、プロパティ名、enum 名)、プロトコルのヘッダディスパッチ。

**設計指針:** 生成コードなら要素数が生成時に分かるため、件数に応じて Equals 連鎖(小)/ ハッシュ switch(中〜)を出し分けるのが理想。

---

## TXT: 文字列・フォーマット

### TXT-01: ルックアップテーブルによる整形・変換

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

---

### TXT-02: 文字列構築の stackalloc ファースト化

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

**リポジトリ内実装:** [ValueStringBuilder.cs](../src/PerformancePatterns/Txt/ValueStringBuilder.cs) / [テスト](../tests/PerformancePatterns.Tests/Txt/ValueStringBuilderTest.cs) / [ベンチマーク](../benchmarks/PerformancePatterns.Benchmarks/Txt/ValueStringBuilderBenchmark.cs) / [測定結果](../benchmarks/results/TXT-02-ValueStringBuilder.md)

**実測結果(Ryzen 9 5900X / net8〜10、24 文字 × 4 連結):** 容量指定なし `StringBuilder` 比で ValueStringBuilder は 0.31〜0.33 倍(約 3.2 倍高速)、アロケーションは 760B → 216B(結果文字列のみ)。stackalloc 付き補間ハンドラとほぼ同速で、容量指定 `StringBuilder`(0.43〜0.47 倍)よりさらに速い。

**ユースケース:** ログメッセージ、キー文字列生成、SQL/パス等の短文組み立て。

**注意:**

- 最低限、`StringBuilder` を使う場合も必ず容量を指定する(それだけで 2.7 倍)
- Grow 処理は JIT-04 に従い NoInlining で分離する

---

### TXT-03: Try パターンによる例外回避

**目的:** 失敗が正常系に含まれる処理(パース・変換・検索)を、例外ではなく bool 戻り値で扱う。

**効果(実測例):**

- `int.Parse` + try/catch は成功時ですら `TryParse` の約 2.5 倍遅い。失敗時は約 540 倍(1,222ns vs 2.27ns)+ 464B のアロケーション
- 例外スロー 1 回のコストは数 μs 規模で、周辺の最適化効果を完全に飲み込む(実測例: 変換失敗パスでは 4.6 倍のキャッシュ最適化差が完全に消えた)

**AOT:** ✅ 問題なし

**設計指針:**

- ライブラリの公開 API は `TryXxx(out T result)` を正とし、例外版(`Xxx`)は Try 版のラッパーとして提供する
- 内部実装でも BCL の Try 系 API(`int.TryParse`, `Utf8Parser.TryParse` 等)を使い、try/catch を制御フローにしない

---

## TYP: 型システム活用

### TYP-01: 静的型スロット(TypeMap / TypeSlot)

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

**注意:** スロット配列の拡張は lock + 配列差し替え(copy-on-write)で行い、読み取りパスをロックフリーに保つ。

---

### TYP-02: BitwiseComparer\<T\>(生バイト比較)

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

**注意:** パディングを含む構造体は未初期化パディングバイトにより「論理的に等しいのに不一致」となる可能性がある。パディングのないレイアウト(または `Pack = 1`)の型に限定して使用する。

---

### TYP-03: UnsafeAccessor(非公開メンバーへの直接アクセス)

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

---

### TYP-04: ジェネリック static クラスによる型別キャッシュ

**目的:** 型ごとに一度だけ計算した成果物(コンバータ、デリゲート、メタデータ)を `static class Cache<T>` の static フィールドに保持し、辞書検索なしで取得する。

**効果(実測例):** `TypeDescriptor.GetConverter` を毎回呼ぶ実装 36.3ns → static キャッシュ 7.96ns(約 4.6 倍)。TYP-01(TypeSlot)はこのパターンの応用形。

**AOT:** ✅ パターン自体は問題なし(ジェネリック static フィールドは AOT 互換)。ただしキャッシュする内容の生成側がリフレクション由来(`TypeDescriptor` 等)の場合は、その API 自体のトリミング対応が別途必要([aot-compatibility.md](aot-compatibility.md) AOTP-05)

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

**注意:** static コンストラクタの初期化は型ごとに初回 1 回のみ。失敗しうる初期化を入れると `TypeInitializationException` が以後もキャッシュされるため、失敗時は「未対応」を表すフォールバック値を入れる設計にする。

---

### TYP-05: Unsafe.As による型チェック省略キャスト

**目的:** 型の対応関係をレジストリ設計で構造的に保証できる場合に、通常キャストの実行時型チェックを `Unsafe.As` で省略する。

**効果(実測例):**

- `(Action<object?>)obj` 3.43ns → `Unsafe.As<Action<object?>>(obj)` 1.59ns(約 2 倍)、コードサイズ 498B → 67B
- DI レジストリの型付き解決(`Resolve<T>`)でも約 1.7 倍 + ジェネリックインスタンス化ごとのキャストコード膨張を抑制

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

## 反パターン:効果がない・逆効果と実測された最適化

「やらない判断」も性能設計の一部。以下は実測で効果なし・逆効果が確認された定番の誤解。AI がコードを生成する際も、これらを「最適化」として適用しないこと。

| 反パターン | 実測結果 | 代わりにやること |
|---|---|---|
| `typeof(X)` の static readonly キャッシュ | 完全に同速(JIT が typeof を定数化) | そのまま `typeof(X)` と書く |
| 単一 Span ループの GetReference + Unsafe.Add 化 | インデクサより 7〜13% 遅い | 通常インデクサ(MEM-01 の適用判断参照) |
| `CollectionsMarshal.AsSpan` 後のさらなる手動 ref ウォーク | 差なし、コードサイズ増のみ | AsSpan 止まりにする |
| ループ構文の選択(for / while / do-while / 昇順・降順) | 差なし | 可読性で選ぶ |
| class 要素の配列への ArrayPool 適用 | 効果なし〜逆効果(要素個別の確保が残る) | 要素の struct 化とセットで(MEM-04) |
| 自前ソート実装 | BCL の `Span.Sort` が約 9 倍速い | BCL のソートを使う |
| 候補 2〜3 文字での `SearchValues` | `IndexOfAny(char, char)` 専用オーバーロードの方が速い | 候補が多い場合のみ SearchValues |
| `FrozenDictionary` の無条件採用 | 構築 15〜20 倍、キー集合によっては検索も逆転 | COL-02 の条件で判断 |
| Span で書ける処理の `fixed` ポインタ化 | `MemoryMarshal.Cast` / `Unsafe.As` と同速か遅い(固定コストが載る) | Span / ref ベースで書く |
| readonly フィールド化による JIT 最適化の期待 | インライン化される限り差は測定不能 | readonly は設計意図として付ける(性能目的にしない) |
| static メソッドを直接バインドしたデリゲートの保持 | thunk 経由で最も遅い呼び出し形態になりうる | DSP-02 の指針で保持形態を選ぶ |
| マイクロベンチ結果の直接外挿 | 単体で 30 倍差でも実処理では 1.1 倍程度に希釈される例あり | 実ワークロード形状で再計測([benchmark-methodology.md](benchmark-methodology.md)) |

---

## 拡充候補パターン(今後追加予定)

上記カタログ(元資料由来)に加え、高性能ライブラリで頻出する以下のパターンを順次ドキュメント化・実装例化する。

| 候補パターン | 概要 | AOT |
|---|---|:---:|
| string.Create / TryFormat / ISpanFormattable | 文字列生成のゼロアロケーション化 | ✅ |
| SearchValues\<T\> | 多数候補探索の SIMD 最適化(.NET 8+)。候補 2〜3 個なら `IndexOfAny` 専用オーバーロードの方が速い点に注意 | ✅ |
| Vector128/256\<T\> ハードウェア組み込み | 明示的 SIMD による一括処理 | ✅ |
| InlineArray | 構造体内固定長バッファ(.NET 8+) | ✅ |
| ObjectPool | 参照型インスタンスの再利用 | ✅ |
| ValueTask / IValueTaskSource | 非同期完了パスのアロケーション削減 | ✅ |
| params ReadOnlySpan\<T\> | 可変長引数の配列アロケーション除去(C# 13) | ✅ |
| Interlocked / lock-free 構造 | 競合の少ない同期プリミティブ設計(.NET 9+ の `System.Threading.Lock` 含む) | ✅ |
| 構造体サイズ別の in / ref 渡し戦略 | 16〜24 バイト超の構造体引数の防御的コピー・値コピー回避 | ✅ |
| P/Invoke バッファの Span 化 | `StringBuilder` マーシャリングを `Span<char>` + ref 渡しに置換 | ✅ |
| XxHash3 による汎用ハッシュ | 非暗号ハッシュの高速化。`MemoryMarshal.Cast` での byte 再解釈はゼロコスト | ✅ |
